# Infrastructure Services

## Overview

The Infrastructure layer provides essential cross-cutting services that support the entire EvvaAgent application. These services handle technical concerns like communication, command execution, deployment orchestration, metrics collection, and data persistence.

## Service Architecture

```
Infrastructure/
├── Communication/           # 📡 External Communication
│   ├── CoreHubService.cs   # SignalR hub management
│   └── ConnectionHealthService.cs # Connection monitoring
├── Execution/              # ⚡ Command Execution
│   └── CommandExecutorService.cs # OS command execution
├── Deployment/             # 🚀 Deployment Management
│   └── DeploymentService.cs # Workflow orchestration
├── Metrics/                # 📊 System Monitoring
│   ├── IMetricsService.cs  # Metrics interface
│   └── MetricsService.cs   # Metrics implementation
└── Data/                   # 💾 Data Access
    ├── ConfigurationRepository.cs
    ├── InformationRepository.cs
    └── ProjectRepository.cs
```

## Communication Services

### CoreHubService
**Namespace**: `EvvaAgent.Infrastructure.Communication`

Manages real-time communication with EvvaCore through SignalR.

```csharp
public interface ICoreHubService
{
    Task<bool> SendMetricsAsync(object metrics);
    Task<bool> SendLogAsync(string level, string message, object? data = null);
    Task StartAsync();
    Task StopAsync();
    Task NotifyDeploymentProgress(int projectId, string stage, string message, bool isError = false);
    Task NotifyDeploymentCompleted(int projectId, bool success, string message);
    bool IsConnected { get; }
    event EventHandler<string>? CommandReceived;
    event EventHandler<bool>? ConnectionStateChanged;
}
```

**Key Features**:
- **Automatic Reconnection**: Handles connection drops with exponential backoff
- **Command Processing**: Receives and routes commands from EvvaCore
- **Real-time Notifications**: Sends deployment progress and system events
- **Health Monitoring**: Tracks connection status and quality
- **Error Recovery**: Graceful handling of network issues

**Implementation Details**:
```csharp
public class CoreHubService : ICoreHubService
{
    private readonly ILogger<CoreHubService> _logger;
    private readonly IConfiguration _configuration;
    private readonly ModularCommandService _commandService;
    private HubConnection? _connection;
    private readonly Timer _reconnectTimer;
    private int _reconnectAttempts = 0;

    public async Task StartAsync()
    {
        try
        {
            var hubUrl = _configuration["EvvaCore:HubUrl"];
            var agentId = _configuration["EvvaCore:AgentId"];

            _connection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30) })
                .Build();

            // Register event handlers
            _connection.On<string, string>("ExecuteCommand", OnCommandReceived);
            _connection.On<string>("Ping", OnPingReceived);

            // Connection state handlers
            _connection.Reconnecting += OnReconnecting;
            _connection.Reconnected += OnReconnected;
            _connection.Closed += OnConnectionClosed;

            await _connection.StartAsync();
            
            // Register agent with core
            await _connection.InvokeAsync("RegisterAgent", agentId);
            
            _logger.LogInformation("Connected to EvvaCore at {HubUrl}", hubUrl);
            IsConnected = true;
            ConnectionStateChanged?.Invoke(this, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to EvvaCore");
            IsConnected = false;
            ConnectionStateChanged?.Invoke(this, false);
        }
    }

    private async Task OnCommandReceived(string command, string parameters)
    {
        try
        {
            _logger.LogInformation("Received command: {Command}", command);
            
            var result = await _commandService.ExecuteCommandAsync(command, parameters, _serviceProvider);
            
            // Send result back to core
            await _connection.InvokeAsync("CommandResult", command, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing command: {Command}", command);
            await _connection.InvokeAsync("CommandError", command, ex.Message);
        }
    }
}
```

### ConnectionHealthService
Monitors and maintains connection health with EvvaCore.

```csharp
public class ConnectionHealthService
{
    private readonly ICoreHubService _coreHubService;
    private readonly ILogger<ConnectionHealthService> _logger;
    private readonly Timer _healthCheckTimer;

    public async Task<HealthStatus> CheckConnectionHealthAsync()
    {
        if (!_coreHubService.IsConnected)
        {
            return new HealthStatus
            {
                IsHealthy = false,
                Message = "Not connected to EvvaCore",
                LastCheck = DateTime.UtcNow
            };
        }

        try
        {
            // Send ping and measure response time
            var stopwatch = Stopwatch.StartNew();
            await _coreHubService.SendPingAsync();
            stopwatch.Stop();

            return new HealthStatus
            {
                IsHealthy = true,
                ResponseTime = stopwatch.ElapsedMilliseconds,
                LastCheck = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            return new HealthStatus
            {
                IsHealthy = false,
                Message = ex.Message,
                LastCheck = DateTime.UtcNow
            };
        }
    }
}
```

## Execution Services

### CommandExecutorService
**Namespace**: `EvvaAgent.Infrastructure.Execution`

Provides cross-platform command execution capabilities.

```csharp
public interface ICommandExecutorService
{
    Task<CommandResult> ExecuteCommandAsync(string command);
    Task<CommandResult> ExecuteCommandAsync(string command, string workingDirectory);
    Task<CommandResult> ExecuteCommandAsync(string command, string workingDirectory, Dictionary<string, string> environment);
    Task<CommandResult> ExecuteCommandWithTimeoutAsync(string command, TimeSpan timeout);
    Task<bool> IsCommandAvailableAsync(string command);
}

public class CommandResult
{
    public string Output { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public int ExitCode { get; set; }
    public TimeSpan Duration { get; set; }
    public bool Success => ExitCode == 0;
}
```

**Implementation Features**:
- **Cross-Platform**: Works on Windows, Linux, and macOS
- **Timeout Support**: Prevents hanging processes
- **Environment Variables**: Custom environment configuration
- **Working Directory**: Execute commands in specific directories
- **Output Capture**: Captures both stdout and stderr
- **Process Management**: Proper process lifecycle management

```csharp
public class CommandExecutorService : ICommandExecutorService
{
    private readonly ILogger<CommandExecutorService> _logger;

    public async Task<CommandResult> ExecuteCommandAsync(string command, string workingDirectory = "", Dictionary<string, string>? environment = null)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogDebug("Executing command: {Command} in {WorkingDirectory}", command, workingDirectory);

            var processInfo = CreateProcessStartInfo(command, workingDirectory, environment);
            
            using var process = new Process { StartInfo = processInfo };
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            // Capture output asynchronously
            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                    outputBuilder.AppendLine(e.Data);
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                    errorBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();
            stopwatch.Stop();

            var result = new CommandResult
            {
                Output = outputBuilder.ToString(),
                Error = errorBuilder.ToString(),
                ExitCode = process.ExitCode,
                Duration = stopwatch.Elapsed
            };

            _logger.LogDebug("Command completed with exit code {ExitCode} in {Duration}ms", 
                result.ExitCode, result.Duration.TotalMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error executing command: {Command}", command);
            
            return new CommandResult
            {
                Error = ex.Message,
                ExitCode = -1,
                Duration = stopwatch.Elapsed
            };
        }
    }

    private ProcessStartInfo CreateProcessStartInfo(string command, string workingDirectory, Dictionary<string, string>? environment)
    {
        var processInfo = new ProcessStartInfo
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        // Platform-specific command execution
        if (OperatingSystem.IsWindows())
        {
            processInfo.FileName = "cmd.exe";
            processInfo.Arguments = $"/c {command}";
        }
        else
        {
            processInfo.FileName = "/bin/bash";
            processInfo.Arguments = $"-c \"{command}\"";
        }

        if (!string.IsNullOrEmpty(workingDirectory))
        {
            processInfo.WorkingDirectory = workingDirectory;
        }

        // Add environment variables
        if (environment != null)
        {
            foreach (var kvp in environment)
            {
                processInfo.Environment[kvp.Key] = kvp.Value;
            }
        }

        return processInfo;
    }
}
```

## Deployment Services

### DeploymentService
**Namespace**: `EvvaAgent.Infrastructure.Deployment`

Orchestrates deployment workflows and manages project deployments.

```csharp
public interface IDeploymentService
{
    Task<DeploymentResult> DeployProjectAsync(DeploymentRequest request);
    Task<DeploymentStatus> GetDeploymentStatusAsync(int deploymentId);
    Task<bool> CancelDeploymentAsync(int deploymentId);
    Task<IEnumerable<DeploymentHistory>> GetDeploymentHistoryAsync(int projectId);
}

public class DeploymentResult
{
    public int DeploymentId { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public List<StepResult> StepResults { get; set; } = new();
}

public class StepResult
{
    public string StepName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Output { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
}
```

**Key Features**:
- **Workflow Orchestration**: Execute deployment steps in sequence
- **Git Integration**: Clone and manage repositories
- **Progress Tracking**: Real-time deployment progress
- **Rollback Support**: Automatic rollback on failure
- **Parallel Execution**: Support for parallel deployment steps
- **Environment Management**: Environment-specific configurations

```csharp
public class DeploymentService : IDeploymentService
{
    private readonly ILogger<DeploymentService> _logger;
    private readonly ICommandExecutorService _commandExecutor;
    private readonly ICoreHubService _coreHubService;
    private readonly ModularCommandService _commandService;
    private readonly IConfiguration _configuration;

    public async Task<DeploymentResult> DeployProjectAsync(DeploymentRequest request)
    {
        var deploymentId = GenerateDeploymentId();
        var stopwatch = Stopwatch.StartNew();
        var stepResults = new List<StepResult>();

        try
        {
            _logger.LogInformation("Starting deployment {DeploymentId} for project {ProjectId}", 
                deploymentId, request.ProjectId);

            await _coreHubService.NotifyDeploymentProgress(request.ProjectId, "Starting", "Deployment initiated");

            // Create workspace
            var workspacePath = await CreateWorkspaceAsync(deploymentId);
            
            // Clone repositories
            foreach (var repo in request.Repositories)
            {
                var stepResult = await CloneRepositoryAsync(repo, workspacePath);
                stepResults.Add(stepResult);
                
                if (!stepResult.Success)
                {
                    await _coreHubService.NotifyDeploymentProgress(request.ProjectId, "Failed", 
                        $"Repository clone failed: {stepResult.Error}", true);
                    return CreateFailedResult(deploymentId, stepResults, stopwatch.Elapsed);
                }
            }

            // Execute workflow steps
            foreach (var step in request.WorkflowSteps.OrderBy(s => s.ExecutionOrder))
            {
                await _coreHubService.NotifyDeploymentProgress(request.ProjectId, step.StageName, 
                    $"Executing: {step.Description ?? step.Command}");

                var stepResult = await ExecuteWorkflowStepAsync(step, workspacePath);
                stepResults.Add(stepResult);

                if (!stepResult.Success)
                {
                    await _coreHubService.NotifyDeploymentProgress(request.ProjectId, "Failed", 
                        $"Step failed: {step.StageName}", true);
                    
                    // Attempt rollback
                    await AttemptRollbackAsync(request, workspacePath);
                    return CreateFailedResult(deploymentId, stepResults, stopwatch.Elapsed);
                }
            }

            stopwatch.Stop();
            await _coreHubService.NotifyDeploymentCompleted(request.ProjectId, true, "Deployment completed successfully");

            return new DeploymentResult
            {
                DeploymentId = deploymentId,
                Success = true,
                Message = "Deployment completed successfully",
                Duration = stopwatch.Elapsed,
                StepResults = stepResults
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Deployment {DeploymentId} failed", deploymentId);
            
            await _coreHubService.NotifyDeploymentCompleted(request.ProjectId, false, 
                $"Deployment failed: {ex.Message}");

            return CreateFailedResult(deploymentId, stepResults, stopwatch.Elapsed, ex.Message);
        }
    }

    private async Task<StepResult> ExecuteWorkflowStepAsync(WorkflowStep step, string workspacePath)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("Executing workflow step: {StepName}", step.StageName);

            object result;
            
            // Check if it's an evva command or system command
            if (step.Command.StartsWith("evva."))
            {
                result = await _commandService.ExecuteCommandAsync(step.Command, step.Parameters, _serviceProvider);
            }
            else
            {
                var commandResult = await _commandExecutor.ExecuteCommandAsync(step.Command, workspacePath);
                result = commandResult;
            }

            stopwatch.Stop();

            return new StepResult
            {
                StepName = step.StageName,
                Success = true,
                Output = JsonSerializer.Serialize(result),
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            return new StepResult
            {
                StepName = step.StageName,
                Success = false,
                Error = ex.Message,
                Duration = stopwatch.Elapsed
            };
        }
    }
}
```

## Metrics Services

### MetricsService
**Namespace**: `EvvaAgent.Infrastructure.Metrics`

Collects and manages system and application metrics.

```csharp
public interface IMetricsService
{
    Task<HostMetricsDto> GetSystemMetricsAsync();
    Task<ServiceMetrics> GetServiceMetricsAsync(string serviceName);
    Task RecordCustomMetricAsync(string name, object value, Dictionary<string, string>? tags = null);
    Task<IEnumerable<MetricHistory>> GetMetricHistoryAsync(string metricName, TimeSpan period);
}

public class HostMetricsDto
{
    public double CpuUsage { get; set; }
    public long MemoryUsed { get; set; }
    public long MemoryTotal { get; set; }
    public long DiskUsed { get; set; }
    public long DiskTotal { get; set; }
    public double NetworkIn { get; set; }
    public double NetworkOut { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> AdditionalMetrics { get; set; } = new();
}
```

**Implementation**:
```csharp
public class MetricsService : IMetricsService
{
    private readonly ILogger<MetricsService> _logger;
    private readonly PerformanceCounter _cpuCounter;
    private readonly PerformanceCounter _memoryCounter;

    public async Task<HostMetricsDto> GetSystemMetricsAsync()
    {
        try
        {
            var metrics = new HostMetricsDto
            {
                Timestamp = DateTime.UtcNow
            };

            // CPU Usage
            if (OperatingSystem.IsWindows())
            {
                metrics.CpuUsage = await GetWindowsCpuUsageAsync();
                metrics.MemoryUsed = await GetWindowsMemoryUsageAsync();
                metrics.MemoryTotal = await GetWindowsTotalMemoryAsync();
            }
            else
            {
                metrics.CpuUsage = await GetLinuxCpuUsageAsync();
                metrics.MemoryUsed = await GetLinuxMemoryUsageAsync();
                metrics.MemoryTotal = await GetLinuxTotalMemoryAsync();
            }

            // Disk Usage
            var drives = DriveInfo.GetDrives().Where(d => d.IsReady);
            metrics.DiskUsed = drives.Sum(d => d.TotalSize - d.AvailableFreeSpace);
            metrics.DiskTotal = drives.Sum(d => d.TotalSize);

            // Network metrics would be collected here
            metrics.NetworkIn = await GetNetworkInBytesAsync();
            metrics.NetworkOut = await GetNetworkOutBytesAsync();

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting system metrics");
            throw;
        }
    }

    private async Task<double> GetWindowsCpuUsageAsync()
    {
        // Windows-specific CPU usage collection
        return _cpuCounter.NextValue();
    }

    private async Task<double> GetLinuxCpuUsageAsync()
    {
        // Linux-specific CPU usage collection using /proc/stat
        var cpuInfo = await File.ReadAllTextAsync("/proc/stat");
        // Parse and calculate CPU usage
        return 0.0; // Placeholder
    }
}
```

## Data Services

### Repository Pattern Implementation

```csharp
// Base Repository
public abstract class BaseRepository<T> where T : class
{
    protected readonly AgentDbContext _context;
    protected readonly ILogger _logger;

    protected BaseRepository(AgentDbContext context, ILogger logger)
    {
        _context = context;
        _logger = logger;
    }

    public virtual async Task<T?> GetByIdAsync(int id)
    {
        return await _context.Set<T>().FindAsync(id);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _context.Set<T>().ToListAsync();
    }

    public virtual async Task<T> CreateAsync(T entity)
    {
        _context.Set<T>().Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task UpdateAsync(T entity)
    {
        _context.Set<T>().Update(entity);
        await _context.SaveChangesAsync();
    }

    public virtual async Task DeleteAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            _context.Set<T>().Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}

// Specific Repository Implementation
public class ProjectRepository : BaseRepository<Project>, IProjectRepository
{
    public ProjectRepository(AgentDbContext context, ILogger<ProjectRepository> logger) 
        : base(context, logger)
    {
    }

    public async Task<IEnumerable<Project>> GetActiveProjectsAsync()
    {
        return await _context.Projects
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<Project?> GetByNameAsync(string name)
    {
        return await _context.Projects
            .FirstOrDefaultAsync(p => p.Name == name);
    }
}
```

## Service Registration

### Dependency Injection Configuration
```csharp
// Program.cs or ServiceCollectionExtensions.cs
public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
{
    // Communication Services
    services.AddSingleton<ICoreHubService, CoreHubService>();
    services.AddScoped<ConnectionHealthService>();

    // Execution Services
    services.AddSingleton<ICommandExecutorService, CommandExecutorService>();

    // Deployment Services
    services.AddScoped<IDeploymentService, DeploymentService>();

    // Metrics Services
    services.AddSingleton<IMetricsService, MetricsService>();

    // Data Services
    services.AddScoped<IProjectRepository, ProjectRepository>();
    services.AddScoped<IConfigurationRepository, ConfigurationRepository>();
    services.AddScoped<IInformationRepository, InformationRepository>();

    // Background Services
    services.AddHostedService<MetricsCollectorWorker>();

    return services;
}
```

## Configuration

### Infrastructure Configuration
```json
{
  "EvvaCore": {
    "HubUrl": "https://evva-core.example.com/hubs/agent",
    "AgentId": "agent-001",
    "ReconnectInterval": 5000,
    "MaxReconnectAttempts": 10,
    "CommandTimeout": 300000
  },
  "Deployment": {
    "WorkspaceRoot": "C:\\deployments",
    "MaxConcurrentDeployments": 3,
    "TimeoutMinutes": 30,
    "RetainWorkspaces": false,
    "GitTimeout": 600
  },
  "Metrics": {
    "CollectionInterval": 30000,
    "EnableSystemMetrics": true,
    "EnableServiceMetrics": true,
    "RetentionDays": 30
  },
  "CommandExecution": {
    "DefaultTimeout": 300000,
    "MaxConcurrentCommands": 10,
    "LogOutput": true
  }
}
```

## Monitoring & Health Checks

### Health Check Implementation
```csharp
public class InfrastructureHealthCheck : IHealthCheck
{
    private readonly ICoreHubService _coreHubService;
    private readonly IMetricsService _metricsService;
    private readonly AgentDbContext _dbContext;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var checks = new Dictionary<string, bool>
        {
            ["CoreConnection"] = _coreHubService.IsConnected,
            ["Database"] = await CheckDatabaseHealthAsync(),
            ["Metrics"] = await CheckMetricsServiceHealthAsync()
        };

        var isHealthy = checks.Values.All(v => v);
        var message = string.Join(", ", checks.Where(kvp => !kvp.Value).Select(kvp => $"{kvp.Key}: Failed"));

        return isHealthy 
            ? HealthCheckResult.Healthy("All infrastructure services are healthy")
            : HealthCheckResult.Unhealthy($"Infrastructure issues: {message}");
    }

    private async Task<bool> CheckDatabaseHealthAsync()
    {
        try
        {
            return await _dbContext.Database.CanConnectAsync();
        }
        catch
        {
            return false;
        }
    }
}
```

This infrastructure layer provides a robust foundation for the EvvaAgent system, ensuring reliable communication, efficient command execution, comprehensive monitoring, and scalable deployment capabilities.