using EvvaAgent.Infrastructure.Execution;
using EvvaAgent.Modules.SystemCtl.Domain;

namespace EvvaAgent.Modules.SystemCtl.Services
{
    public class SystemCtlService
    {
        private readonly ILogger<SystemCtlService> _logger;
        private readonly ICommandExecutorService _commandExecutor;

        public SystemCtlService(ILogger<SystemCtlService> logger, ICommandExecutorService commandExecutor)
        {
            _logger = logger;
            _commandExecutor = commandExecutor;
        }

        public async Task<ServiceOperationResult> CreateServiceAsync(ServiceConfig config)
        {
            try
            {
                var serviceContent = GenerateServiceFileContent(config);
                var serviceFilePath = $"/etc/systemd/system/{config.ServiceName}.service";
                
                var createFileCommand = $"echo '{serviceContent}' | sudo tee {serviceFilePath}";
                var createResult = await _commandExecutor.ExecuteCommandAsync(createFileCommand);
                
                if (createResult.ExitCode != 0)
                {
                    return new ServiceOperationResult
                    {
                        Success = false,
                        ServiceName = config.ServiceName,
                        Operation = "create",
                        Message = "Failed to create service file",
                        Error = createResult.Error
                    };
                }
                
                await _commandExecutor.ExecuteCommandAsync("sudo systemctl daemon-reload");
                
                if (config.StartType == ServiceStartType.Automatic)
                {
                    await _commandExecutor.ExecuteCommandAsync($"sudo systemctl enable {config.ServiceName}");
                }
                
                return new ServiceOperationResult
                {
                    Success = true,
                    ServiceName = config.ServiceName,
                    Operation = "create",
                    Message = "Service created successfully",
                    Output = createResult.Output
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating service {ServiceName}", config.ServiceName);
                return new ServiceOperationResult
                {
                    Success = false,
                    ServiceName = config.ServiceName,
                    Operation = "create",
                    Message = "Error creating service",
                    Error = ex.Message
                };
            }
        }
        
        private string GenerateServiceFileContent(ServiceConfig config)
        {
            var content = "[Unit]\n";
            content += $"Description={config.Description ?? config.DisplayName}\n";
            content += "After=network.target\n\n";
            
            content += "[Service]\n";
            content += "Type=simple\n";
            content += $"ExecStart={config.ExecutablePath}\n";
            
            if (!string.IsNullOrEmpty(config.WorkingDirectory))
            {
                content += $"WorkingDirectory={config.WorkingDirectory}\n";
            }
            
            if (!string.IsNullOrEmpty(config.Username))
            {
                content += $"User={config.Username}\n";
            }
            
            content += "Restart=always\n";
            content += "RestartSec=10\n\n";
            
            content += "[Install]\n";
            content += "WantedBy=multi-user.target\n";
            
            return content;
        }

        public async Task<ServiceOperationResult> StartServiceAsync(string serviceName)
        {
            try
            {
                _logger.LogInformation("Starting service: {ServiceName}", serviceName);
                
                var result = await _commandExecutor.ExecuteCommandAsync($"systemctl start {serviceName}");
                
                return new ServiceOperationResult
                {
                    Success = result.ExitCode == 0,
                    ServiceName = serviceName,
                    Operation = "start",
                    Message = result.ExitCode == 0 ? "Service started successfully" : "Failed to start service",
                    Output = result.Output,
                    Error = result.Error
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting service: {ServiceName}", serviceName);
                return new ServiceOperationResult
                {
                    Success = false,
                    ServiceName = serviceName,
                    Operation = "start",
                    Message = ex.Message
                };
            }
        }

        public async Task<ServiceOperationResult> StopServiceAsync(string serviceName)
        {
            try
            {
                _logger.LogInformation("Stopping service: {ServiceName}", serviceName);
                
                var result = await _commandExecutor.ExecuteCommandAsync($"systemctl stop {serviceName}");
                
                return new ServiceOperationResult
                {
                    Success = result.ExitCode == 0,
                    ServiceName = serviceName,
                    Operation = "stop",
                    Message = result.ExitCode == 0 ? "Service stopped successfully" : "Failed to stop service",
                    Output = result.Output,
                    Error = result.Error
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping service: {ServiceName}", serviceName);
                return new ServiceOperationResult
                {
                    Success = false,
                    ServiceName = serviceName,
                    Operation = "stop",
                    Message = ex.Message
                };
            }
        }

        public async Task<ServiceOperationResult> RestartServiceAsync(string serviceName)
        {
            try
            {
                _logger.LogInformation("Restarting service: {ServiceName}", serviceName);
                
                var result = await _commandExecutor.ExecuteCommandAsync($"systemctl restart {serviceName}");
                
                return new ServiceOperationResult
                {
                    Success = result.ExitCode == 0,
                    ServiceName = serviceName,
                    Operation = "restart",
                    Message = result.ExitCode == 0 ? "Service restarted successfully" : "Failed to restart service",
                    Output = result.Output,
                    Error = result.Error
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restarting service: {ServiceName}", serviceName);
                return new ServiceOperationResult
                {
                    Success = false,
                    ServiceName = serviceName,
                    Operation = "restart",
                    Message = ex.Message
                };
            }
        }

        public async Task<ServiceStatus> GetServiceStatusAsync(string serviceName)
        {
            try
            {
                _logger.LogInformation("Getting status for service: {ServiceName}", serviceName);
                
                var result = await _commandExecutor.ExecuteCommandAsync($"systemctl show {serviceName} --no-page");
                
                if (result.ExitCode == 0)
                {
                    return ParseServiceStatus(serviceName, result.Output);
                }

                return new ServiceStatus
                {
                    ServiceName = serviceName,
                    Status = "unknown",
                    IsActive = false,
                    IsEnabled = false
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service status: {ServiceName}", serviceName);
                return new ServiceStatus
                {
                    ServiceName = serviceName,
                    Status = "error",
                    IsActive = false,
                    IsEnabled = false
                };
            }
        }

        public async Task<ServiceOperationResult> EnableServiceAsync(string serviceName)
        {
            try
            {
                _logger.LogInformation("Enabling service: {ServiceName}", serviceName);
                
                var result = await _commandExecutor.ExecuteCommandAsync($"systemctl enable {serviceName}");
                
                return new ServiceOperationResult
                {
                    Success = result.ExitCode == 0,
                    ServiceName = serviceName,
                    Operation = "enable",
                    Message = result.ExitCode == 0 ? "Service enabled successfully" : "Failed to enable service",
                    Output = result.Output,
                    Error = result.Error
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enabling service: {ServiceName}", serviceName);
                return new ServiceOperationResult
                {
                    Success = false,
                    ServiceName = serviceName,
                    Operation = "enable",
                    Message = ex.Message
                };
            }
        }

        public async Task<ServiceOperationResult> DisableServiceAsync(string serviceName)
        {
            try
            {
                _logger.LogInformation("Disabling service: {ServiceName}", serviceName);
                
                var result = await _commandExecutor.ExecuteCommandAsync($"systemctl disable {serviceName}");
                
                return new ServiceOperationResult
                {
                    Success = result.ExitCode == 0,
                    ServiceName = serviceName,
                    Operation = "disable",
                    Message = result.ExitCode == 0 ? "Service disabled successfully" : "Failed to disable service",
                    Output = result.Output,
                    Error = result.Error
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disabling service: {ServiceName}", serviceName);
                return new ServiceOperationResult
                {
                    Success = false,
                    ServiceName = serviceName,
                    Operation = "disable",
                    Message = ex.Message
                };
            }
        }

        private ServiceStatus ParseServiceStatus(string serviceName, string output)
        {
            var status = new ServiceStatus { ServiceName = serviceName };
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var parts = line.Split('=', 2);
                if (parts.Length != 2) continue;

                var key = parts[0].Trim();
                var value = parts[1].Trim();

                switch (key)
                {
                    case "ActiveState":
                        status.IsActive = value == "active";
                        status.Status = value;
                        break;
                    case "UnitFileState":
                        status.IsEnabled = value == "enabled";
                        break;
                    case "SubState":
                        status.SubState = value;
                        break;
                    case "Description":
                        status.Description = value;
                        break;
                    case "MainPID":
                        if (int.TryParse(value, out var pid) && pid > 0)
                            status.ProcessId = pid;
                        break;
                    case "MemoryCurrent":
                        if (long.TryParse(value, out var memory))
                            status.MemoryUsage = memory;
                        break;
                    case "ActiveEnterTimestamp":
                        if (DateTime.TryParse(value, out var startTime))
                            status.StartTime = startTime;
                        break;
                }
            }

            return status;
        }
    }
}