using System.Text.Json;
using System.Text.Json.Serialization;
using EvvaAgent.Core.Abstractions;
using EvvaAgent.Modules.SystemCtl.Domain;
using EvvaAgent.Modules.SystemCtl.Services;

namespace EvvaAgent.Modules.SystemCtl.Resources
{
    public class SystemCtlResource : IEvvaResource
    {
        private readonly Dictionary<string, Func<string?, IServiceProvider, Task<object>>> _methods;

        public SystemCtlResource()
        {
            _methods = new Dictionary<string, Func<string?, IServiceProvider, Task<object>>>
            {
                ["create"] = ExecuteCreateAsync,
                ["start"] = ExecuteStartAsync,
                ["stop"] = ExecuteStopAsync,
                ["restart"] = ExecuteRestartAsync,
                ["status"] = ExecuteStatusAsync,
                ["enable"] = ExecuteEnableAsync,
                ["disable"] = ExecuteDisableAsync
            };
        }

        public async Task<object> ExecuteAsync(string method, string? parameters, IServiceProvider serviceProvider)
        {
            if (!_methods.ContainsKey(method))
            {
                return new { success = false, error = $"Method '{method}' not found" };
            }

            try
            {
                return await _methods[method](parameters, serviceProvider);
            }
            catch (Exception ex)
            {
                return new { success = false, error = ex.Message };
            }
        }

        public IEnumerable<string> GetAvailableMethods()
        {
            return _methods.Keys;
        }

        private async Task<object> ExecuteStartAsync(string? parameters, IServiceProvider serviceProvider)
        {
            if (string.IsNullOrEmpty(parameters))
            {
                return new { success = false, error = "Service name is required" };
            }

            using var scope = serviceProvider.CreateScope();
            var systemCtlService = scope.ServiceProvider.GetRequiredService<SystemCtlService>();

            var result = await systemCtlService.StartServiceAsync(parameters);
            return new 
            { 
                success = result.Success, 
                action = "start_service", 
                service = result.ServiceName,
                message = result.Message,
                output = result.Output,
                error = result.Error
            };
        }

        private async Task<object> ExecuteStopAsync(string? parameters, IServiceProvider serviceProvider)
        {
            if (string.IsNullOrEmpty(parameters))
            {
                return new { success = false, error = "Service name is required" };
            }

            using var scope = serviceProvider.CreateScope();
            var systemCtlService = scope.ServiceProvider.GetRequiredService<SystemCtlService>();

            var result = await systemCtlService.StopServiceAsync(parameters);
            return new 
            { 
                success = result.Success, 
                action = "stop_service", 
                service = result.ServiceName,
                message = result.Message,
                output = result.Output,
                error = result.Error
            };
        }

        private async Task<object> ExecuteRestartAsync(string? parameters, IServiceProvider serviceProvider)
        {
            if (string.IsNullOrEmpty(parameters))
            {
                return new { success = false, error = "Service name is required" };
            }

            using var scope = serviceProvider.CreateScope();
            var systemCtlService = scope.ServiceProvider.GetRequiredService<SystemCtlService>();

            var result = await systemCtlService.RestartServiceAsync(parameters);
            return new 
            { 
                success = result.Success, 
                action = "restart_service", 
                service = result.ServiceName,
                message = result.Message,
                output = result.Output,
                error = result.Error
            };
        }

        private async Task<object> ExecuteStatusAsync(string? parameters, IServiceProvider serviceProvider)
        {
            if (string.IsNullOrEmpty(parameters))
            {
                return new { success = false, error = "Service name is required" };
            }

            using var scope = serviceProvider.CreateScope();
            var systemCtlService = scope.ServiceProvider.GetRequiredService<SystemCtlService>();

            var status = await systemCtlService.GetServiceStatusAsync(parameters);
            return new 
            { 
                success = true, 
                action = "get_service_status", 
                service = status.ServiceName,
                data = status
            };
        }

        private async Task<object> ExecuteEnableAsync(string? parameters, IServiceProvider serviceProvider)
        {
            if (string.IsNullOrEmpty(parameters))
            {
                return new { success = false, error = "Service name is required" };
            }

            using var scope = serviceProvider.CreateScope();
            var systemCtlService = scope.ServiceProvider.GetRequiredService<SystemCtlService>();

            var result = await systemCtlService.EnableServiceAsync(parameters);
            return new 
            { 
                success = result.Success, 
                action = "enable_service", 
                service = result.ServiceName,
                message = result.Message,
                output = result.Output,
                error = result.Error
            };
        }

        private async Task<object> ExecuteCreateAsync(string? parameters, IServiceProvider serviceProvider)
        {
            if (string.IsNullOrEmpty(parameters))
            {
                return new { success = false, error = "Service configuration is required" };
            }

            var config = JsonSerializer.Deserialize<ServiceConfig>(parameters, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            });
            if (config == null)
            {
                return new { success = false, error = "Invalid service configuration" };
            }

            using var scope = serviceProvider.CreateScope();
            var systemCtlService = scope.ServiceProvider.GetRequiredService<SystemCtlService>();

            var result = await systemCtlService.CreateServiceAsync(config);
            return new 
            { 
                success = result.Success, 
                action = "create_service", 
                service = result.ServiceName,
                message = result.Message,
                output = result.Output,
                error = result.Error
            };
        }

        private async Task<object> ExecuteDisableAsync(string? parameters, IServiceProvider serviceProvider)
        {
            if (string.IsNullOrEmpty(parameters))
            {
                return new { success = false, error = "Service name is required" };
            }

            using var scope = serviceProvider.CreateScope();
            var systemCtlService = scope.ServiceProvider.GetRequiredService<SystemCtlService>();

            var result = await systemCtlService.DisableServiceAsync(parameters);
            return new 
            { 
                success = result.Success, 
                action = "disable_service", 
                service = result.ServiceName,
                message = result.Message,
                output = result.Output,
                error = result.Error
            };
        }
    }
}