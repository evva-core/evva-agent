using Microsoft.AspNetCore.SignalR.Client;
using EvvaAgent.Infrastructure.Execution;
using EvvaAgent.Domain;
using EvvaAgent.Core.Commands;
using EvvaAgent.Domain.Repositories;

namespace EvvaAgent.Infrastructure.Communication;

public interface ICoreHubService
{
    Task<bool> SendMetricsAsync(object metrics);
    Task StartAsync();
    Task StopAsync();
    Task NotifyDeploymentProgress(int projectId, string stage, string message, bool isError = false);
    Task NotifyDeploymentCompleted(int projectId, bool success, string message);
    bool IsConnected { get; }
}

public class CoreHubService : ICoreHubService, IDisposable
{
    private readonly ILogger<CoreHubService> _logger;
    private readonly ICommandExecutorService _commandExecutor;
    private readonly IServiceProvider _serviceProvider;
    private readonly IInformationRepository _informationRepository;
    private readonly IConfigurationRepository _configurationRepository;
    private string _uniqueId = string.Empty;
    private string _coreUrl = string.Empty;
    private HubConnection _connection;

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;
    
    public CoreHubService(ILogger<CoreHubService> logger, ICommandExecutorService commandExecutor, IServiceProvider serviceProvider, IInformationRepository informationRepository, IConfigurationRepository configurationRepository)
    {
        _logger = logger;
        _commandExecutor = commandExecutor;
        _serviceProvider = serviceProvider;
        _informationRepository = informationRepository;
        _configurationRepository = configurationRepository;

    }

    private async Task InitializeConnectionAsync()
    {
        _logger.LogInformation("Getting information from database...");
        var information = await _informationRepository.GetAsync();
        _logger.LogInformation("Information UUID: {Uuid}", information?.Uuid ?? "null");
        
        if (information == null || string.IsNullOrEmpty(information.Uuid))
            throw new InvalidOperationException("UUID not found in database. Please configure the agent first.");

        _logger.LogInformation("Getting configuration from database...");
        var configuration = await _configurationRepository.GetAsync();
        _logger.LogInformation("Configuration URL: {Url}", configuration?.AdminServerUrl ?? "null");
        
        if (configuration == null || string.IsNullOrEmpty(configuration.AdminServerUrl))
            throw new InvalidOperationException("Admin server URL not found in database. Please configure the agent first.");

        _uniqueId = information.Uuid;
        _coreUrl = $"{configuration.AdminServerUrl}/hostHub";
        
        _logger.LogInformation("Building connection to: {CoreUrl}", _coreUrl);

        _connection = new HubConnectionBuilder()
            .WithUrl(_coreUrl)
            .WithAutomaticReconnect()
            .Build();

        // Escuta comandos do core
        _connection.On<object>("ExecuteClone", async (data) =>
        {
            System.Console.WriteLine("Received ExecuteClone command");
            await HandleCloneCommand(data);
        });
        
        _connection.On<object>("ExecuteDeployment", async (data) =>
        {
            System.Console.WriteLine("Received ExecuteDeployment command");
            await HandleDeploymentCommand(data);
        });
        
        _connection.On<string, string>("ExecuteEvvaCommand", async (commandKey, parameters) =>
        {
            System.Console.WriteLine($"Received Evva command: {commandKey}");
            await HandleEvvaCommand(commandKey, parameters);
        });
    }

    public async Task StartAsync()
    {
        
        try
        {

            await InitializeConnectionAsync();
            
            if (_connection.State == HubConnectionState.Disconnected)
            {
                _logger.LogInformation("Starting SignalR connection to {CoreUrl}", _coreUrl);
                await _connection.StartAsync();
                await _connection.InvokeAsync("JoinHostGroup", _uniqueId);
                _logger.LogInformation("Connected to core at {CoreUrl} and joined group: {UniqueId}", _coreUrl, _uniqueId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to core: {Message}", ex.Message);
        }
    }

    public async Task<bool> SendMetricsAsync(object metrics)
    {
        try
        {
            if (IsConnected)
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await _connection.InvokeAsync("SendHostData", _uniqueId, metrics);
                return true;
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("SendMetrics timeout after 5 seconds");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send metrics");
        }
        return false;
    }

    private async Task HandleCloneCommand(object data)
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(data);
            var cloneData = System.Text.Json.JsonSerializer.Deserialize<CloneData>(json);
    
            if (cloneData != null)
            {
                var command = $"git clone -b {cloneData.branch} {cloneData.url} {cloneData.targetPath}";
                var result = await _commandExecutor.ExecuteCommandAsync(command);
                System.Console.WriteLine($"Output: {result.Output}\nError: {result.Error}\nExitCode: {result.ExitCode}");
                
                try
                {
                    await _connection.InvokeAsync("SendHostData", _uniqueId, new 
                    { 
                        type = "repository_clone_completed", 
                        repositoryId = cloneData.repositoryId, 
                        success = result.ExitCode != 1, 
                        result = $"Output: {result.Output}\nError: {result.Error}" 
                    });
                }
                catch (Exception invokeEx)
                {
                    _logger.LogError(invokeEx, "Failed to send clone completion notification");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing clone command");
            try
            {
                await _connection.InvokeAsync("SendHostData", _uniqueId, new 
                { 
                    type = "repository_clone_failed", 
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception invokeEx)
            {
                _logger.LogError(invokeEx, "Failed to send clone error notification");
            }
        }
    }

    public async Task StopAsync()
    {
        if (_connection != null)
        {
            await _connection.StopAsync();
        }
    }

    public async Task NotifyDeploymentProgress(int projectId, string stage, string message, bool isError = false)
    {
        try
        {
            if (IsConnected)
            {
                await _connection.InvokeAsync("SendHostData", _uniqueId, new 
                { 
                    type = "deployment_progress", 
                    projectId, 
                    stage, 
                    message, 
                    isError, 
                    timestamp = DateTime.UtcNow 
                });
            }
            else
            {
                _logger.LogWarning("Cannot send deployment progress - not connected to core");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send deployment progress for project {ProjectId}", projectId);
        }
    }

    public async Task NotifyDeploymentCompleted(int projectId, bool success, string message)
    {
        try
        {
            if (IsConnected)
            {
                await _connection.InvokeAsync("SendHostData", _uniqueId, new 
                { 
                    type = "deployment_completed", 
                    projectId, 
                    success, 
                    message, 
                    timestamp = DateTime.UtcNow 
                });
            }
            else
            {
                _logger.LogWarning("Cannot send deployment completion - not connected to core");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send deployment completion for project {ProjectId}", projectId);
        }
    }

    private async Task HandleDeploymentCommand(object data)
    {
        int? projectId = null;
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(data);
            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var deploymentRequest = System.Text.Json.JsonSerializer.Deserialize<DeploymentRequest>(json, options);
            
            if (deploymentRequest == null)
            {
                _logger.LogError("Failed to deserialize deployment request");
                return;
            }
            
            projectId = deploymentRequest.ProjectId;
            await NotifyDeploymentProgress(deploymentRequest.ProjectId, "Starting", "Deployment process initiated");

            // Clone/sync repositories
            foreach (var repo in deploymentRequest.Repositories)
            {
                try
                {
                    await NotifyDeploymentProgress(deploymentRequest.ProjectId, "Repository", $"Processing repository: {repo.Name}");
                    
                    var cloneCommand = $"git clone -b {repo.Branch} {repo.RepositoryUrl} {repo.TargetPath}";
                    var cloneResult = await _commandExecutor.ExecuteCommandAsync(cloneCommand);
                    
                    if (cloneResult.ExitCode == 1)
                    {
                        await NotifyDeploymentProgress(deploymentRequest.ProjectId, "Repository", $"Repository clone failed: {cloneResult.Error}", true);
                        await NotifyDeploymentCompleted(deploymentRequest.ProjectId, false, $"Failed to clone repository: {repo.Name}");
                        return;
                    }
                }
                catch (Exception repoEx)
                {
                    _logger.LogError(repoEx, "Error processing repository: {RepoName}", repo.Name);
                    await NotifyDeploymentProgress(deploymentRequest.ProjectId, "Repository", $"Repository error: {repoEx.Message}", true);
                    await NotifyDeploymentCompleted(deploymentRequest.ProjectId, false, $"Repository processing failed: {repo.Name}");
                    return;
                }
            }
            
            // Execute workflow steps
            foreach (var step in deploymentRequest.WorkflowSteps.OrderBy(w => w.ExecutionOrder))
            {
                try
                {
                    await NotifyDeploymentProgress(deploymentRequest.ProjectId, step.StageName, $"Executing: {step.WorkflowName}");
                    
                    // Check if this is an Evva internal command
                    if (step.Command.StartsWith("evva."))
                    {
                        var success = await ExecuteEvvaWorkflowStep(step);
                        if (!success)
                        {
                            await NotifyDeploymentProgress(deploymentRequest.ProjectId, step.StageName, $"Evva command failed: {step.Command}", true);
                            await NotifyDeploymentCompleted(deploymentRequest.ProjectId, false, $"Deployment failed at Evva step: {step.WorkflowName}");
                            return;
                        }
                    }
                    else
                    {
                        // Regular shell command
                        var targetRepo = deploymentRequest.Repositories.FirstOrDefault(r => 
                            step.Description?.Contains(r.Name, StringComparison.OrdinalIgnoreCase) == true) 
                            ?? deploymentRequest.Repositories.FirstOrDefault();
                        
                        var workingDir = targetRepo?.TargetPath;
                        var result = await _commandExecutor.ExecuteCommandInDirectoryAsync(step.Command, workingDir ?? Environment.CurrentDirectory);
                        
                        if (result.ExitCode == 1)
                        {
                            await NotifyDeploymentProgress(deploymentRequest.ProjectId, step.StageName, $"Step failed: {result.Output}\n{result.Error}", true);
                            await NotifyDeploymentCompleted(deploymentRequest.ProjectId, false, $"Deployment failed at step: {step.WorkflowName}");
                            return;
                        }
                    }
                }
                catch (Exception stepEx)
                {
                    _logger.LogError(stepEx, "Error executing workflow step: {StepName}", step.WorkflowName);
                    await NotifyDeploymentProgress(deploymentRequest.ProjectId, step.StageName, $"Step error: {stepEx.Message}", true);
                    await NotifyDeploymentCompleted(deploymentRequest.ProjectId, false, $"Step execution failed: {step.WorkflowName}");
                    return;
                }
            }

            await NotifyDeploymentCompleted(deploymentRequest.ProjectId, true, "Deployment completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error in deployment command handler");
            if (projectId.HasValue)
            {
                try
                {
                    await NotifyDeploymentCompleted(projectId.Value, false, $"Deployment failed with critical error: {ex.Message}");
                }
                catch (Exception notifyEx)
                {
                    _logger.LogError(notifyEx, "Failed to send deployment failure notification");
                }
            }
        }
    }

    private async Task<bool> ExecuteEvvaWorkflowStep(dynamic step)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var evvaCommandService = scope.ServiceProvider.GetRequiredService<ModularCommandService>();
            
            var commandKey = step.Command;
            
            // Determine which data to use based on IsJsonRequired flag
            string? parameterData = null;
            if (step.IsJsonRequired == true)
            {
                parameterData = step.JsonData;
            }
            else
            {
                parameterData = step.Parameters;
            }
            
            var result = await evvaCommandService.ExecuteCommandAsync(commandKey, parameterData, _serviceProvider);
            
            // Check if result indicates success
            var resultJson = System.Text.Json.JsonSerializer.Serialize(result);
            var resultObj = System.Text.Json.JsonSerializer.Deserialize<dynamic>(resultJson);
            
            return resultObj?.GetProperty("success").GetBoolean() ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing evva workflow step: {Command}", (string)step.Command);
            return false;
        }
    }

    private async Task HandleEvvaCommand(string commandKey, string parameters)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var evvaCommandService = scope.ServiceProvider.GetRequiredService<ModularCommandService>();
            
            var result = await evvaCommandService.ExecuteCommandAsync(commandKey, parameters, _serviceProvider);
            
            await _connection.InvokeAsync("SendHostData", _uniqueId, new 
            { 
                type = "evva_command_completed", 
                commandKey, 
                result,
                timestamp = DateTime.UtcNow 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing evva command: {CommandKey}", commandKey);
            await _connection.InvokeAsync("SendHostData", _uniqueId, new 
            { 
                type = "evva_command_failed", 
                commandKey, 
                error = ex.Message,
                timestamp = DateTime.UtcNow 
            });
        }
    }

    public void Dispose()
    {
        _connection?.DisposeAsync();
    }
}

public class CloneData
{
    public int repositoryId { get; set; }
    public string url { get; set; } = string.Empty;
    public string branch { get; set; } = string.Empty;
    public string targetPath { get; set; } = string.Empty;
}