using System.Diagnostics;
using System.ServiceProcess;
using EvvaAgent.Infrastructure.Execution;
using EvvaAgent.Modules.WindowsService.Domain;
using Microsoft.Extensions.Logging;

namespace EvvaAgent.Modules.WindowsService.Services
{
    public class WindowsServiceService
    {
        private readonly ICommandExecutorService _commandExecutor;
        private readonly ILogger<WindowsServiceService> _logger;

        public WindowsServiceService(ICommandExecutorService commandExecutor, ILogger<WindowsServiceService> logger)
        {
            _commandExecutor = commandExecutor;
            _logger = logger;
        }

        public async Task<ServiceOperationResult> CreateServiceAsync(ServiceConfig config)
        {
            try
            {
                var startType = config.StartType switch
                {
                    ServiceStartType.Automatic => "auto",
                    ServiceStartType.Manual => "demand",
                    ServiceStartType.Disabled => "disabled",
                    _ => "demand"
                };

                var command = $"sc create \"{config.ServiceName}\" binPath=\"{config.ExecutablePath}\" DisplayName=\"{config.DisplayName}\" start={startType}";
                
                if (!string.IsNullOrEmpty(config.Description))
                {
                    command += $" && sc description \"{config.ServiceName}\" \"{config.Description}\"";
                }

                var result = await _commandExecutor.ExecuteCommandAsync(command);
                if(result.ExitCode == 0)
                {
                    await StartServiceAsync(config.ServiceName);
                }
                return new ServiceOperationResult
                {
                    Success = result.ExitCode == 0,
                    ServiceName = config.ServiceName,
                    Message = result.ExitCode == 0 ? "Service created successfully" : "Failed to create service",
                    Output = result.Output,
                    Error = result.Error
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating service {ServiceName}", config.ServiceName);
                return new ServiceOperationResult
                {
                    Success = false,
                    ServiceName = config.ServiceName,
                    Message = "Error creating service",
                    Error = ex.Message
                };
            }
        }

        public async Task<ServiceOperationResult> StartServiceAsync(string serviceName)
        {
            try
            {
                var result = await _commandExecutor.ExecuteCommandAsync($"sc start \"{serviceName}\"");
                
                return new ServiceOperationResult
                {
                    Success = result.ExitCode == 0,
                    ServiceName = serviceName,
                    Message = result.ExitCode == 0 ? "Service started successfully" : "Failed to start service",
                    Output = result.Output,
                    Error = result.Error
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting service {ServiceName}", serviceName);
                return new ServiceOperationResult
                {
                    Success = false,
                    ServiceName = serviceName,
                    Message = "Error starting service",
                    Error = ex.Message
                };
            }
        }

        public async Task<ServiceOperationResult> StopServiceAsync(string serviceName)
        {
            try
            {
                var result = await _commandExecutor.ExecuteCommandAsync($"sc stop \"{serviceName}\"");
                
                return new ServiceOperationResult
                {
                    Success = result.ExitCode == 0,
                    ServiceName = serviceName,
                    Message = result.ExitCode == 0 ? "Service stopped successfully" : "Failed to stop service",
                    Output = result.Output,
                    Error = result.Error
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping service {ServiceName}", serviceName);
                return new ServiceOperationResult
                {
                    Success = false,
                    ServiceName = serviceName,
                    Message = "Error stopping service",
                    Error = ex.Message
                };
            }
        }

        public async Task<ServiceOperationResult> RestartServiceAsync(string serviceName)
        {
            try
            {
                var stopResult = await StopServiceAsync(serviceName);
                if (!stopResult.Success)
                {
                    return stopResult;
                }

                await Task.Delay(2000); // Wait 2 seconds

                return await StartServiceAsync(serviceName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restarting service {ServiceName}", serviceName);
                return new ServiceOperationResult
                {
                    Success = false,
                    ServiceName = serviceName,
                    Message = "Error restarting service",
                    Error = ex.Message
                };
            }
        }

        public async Task<ServiceOperationResult> DeleteServiceAsync(string serviceName)
        {
            try
            {
                // Stop service first
                await StopServiceAsync(serviceName);
                await Task.Delay(1000);

                var result = await _commandExecutor.ExecuteCommandAsync($"sc delete \"{serviceName}\"");
                
                return new ServiceOperationResult
                {
                    Success = result.ExitCode == 0,
                    ServiceName = serviceName,
                    Message = result.ExitCode == 0 ? "Service deleted successfully" : "Failed to delete service",
                    Output = result.Output,
                    Error = result.Error
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting service {ServiceName}", serviceName);
                return new ServiceOperationResult
                {
                    Success = false,
                    ServiceName = serviceName,
                    Message = "Error deleting service",
                    Error = ex.Message
                };
            }
        }

        public async Task<ServiceStatus> GetServiceStatusAsync(string serviceName)
        {
            try
            {
                var result = await _commandExecutor.ExecuteCommandAsync($"sc query \"{serviceName}\"");
                
                var status = new ServiceStatus
                {
                    ServiceName = serviceName,
                    DisplayName = serviceName
                };

                if (result.ExitCode == 0 && !string.IsNullOrEmpty(result.Output))
                {
                    var output = result.Output;
                    
                    if (output.Contains("RUNNING"))
                    {
                        status.Status = "Running";
                        status.IsRunning = true;
                    }
                    else if (output.Contains("STOPPED"))
                    {
                        status.Status = "Stopped";
                        status.IsRunning = false;
                    }
                    else
                    {
                        status.Status = "Unknown";
                        status.IsRunning = false;
                    }
                }
                else
                {
                    status.Status = "Not Found";
                    status.IsRunning = false;
                }

                return status;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service status {ServiceName}", serviceName);
                return new ServiceStatus
                {
                    ServiceName = serviceName,
                    Status = "Error",
                    IsRunning = false
                };
            }
        }
    }
}