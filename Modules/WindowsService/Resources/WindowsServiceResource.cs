using System.Text.Json;
using System.Text.Json.Serialization;
using EvvaAgent.Core.Abstractions;
using EvvaAgent.Modules.WindowsService.Domain;
using EvvaAgent.Modules.WindowsService.Services;

namespace EvvaAgent.Modules.WindowsService.Resources
{
    public class WindowsServiceResource : IEvvaResource
    {
        private readonly Dictionary<string, Func<string?, IServiceProvider, Task<object>>> _methods;
        private readonly JsonSerializerOptions _jsonOptions;

        public WindowsServiceResource()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };

            _methods = new Dictionary<string, Func<string?, IServiceProvider, Task<object>>>
            {
                ["create"] = ExecuteCreateAsync,
                ["start"] = ExecuteStartAsync,
                ["stop"] = ExecuteStopAsync,
                ["restart"] = ExecuteRestartAsync,
                ["delete"] = ExecuteDeleteAsync,
                ["status"] = ExecuteStatusAsync
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

        private async Task<object> ExecuteCreateAsync(string? parameters, IServiceProvider serviceProvider)
        {
            if (string.IsNullOrEmpty(parameters))
            {
                return new { success = false, error = "Service configuration is required" };
            }

            var config = JsonSerializer.Deserialize<ServiceConfig>(parameters, _jsonOptions);
            if (config == null)
            {
                return new { success = false, error = "Invalid service configuration" };
            }

            using var scope = serviceProvider.CreateScope();
            var windowsServiceService = scope.ServiceProvider.GetRequiredService<WindowsServiceService>();

            var result = await windowsServiceService.CreateServiceAsync(config);
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

        private async Task<object> ExecuteStartAsync(string? parameters, IServiceProvider serviceProvider)
        {
            if (string.IsNullOrEmpty(parameters))
            {
                return new { success = false, error = "Service name is required" };
            }

            using var scope = serviceProvider.CreateScope();
            var windowsServiceService = scope.ServiceProvider.GetRequiredService<WindowsServiceService>();

            var result = await windowsServiceService.StartServiceAsync(parameters);
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
            var windowsServiceService = scope.ServiceProvider.GetRequiredService<WindowsServiceService>();

            var result = await windowsServiceService.StopServiceAsync(parameters);
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
            var windowsServiceService = scope.ServiceProvider.GetRequiredService<WindowsServiceService>();

            var result = await windowsServiceService.RestartServiceAsync(parameters);
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

        private async Task<object> ExecuteDeleteAsync(string? parameters, IServiceProvider serviceProvider)
        {
            if (string.IsNullOrEmpty(parameters))
            {
                return new { success = false, error = "Service name is required" };
            }

            using var scope = serviceProvider.CreateScope();
            var windowsServiceService = scope.ServiceProvider.GetRequiredService<WindowsServiceService>();

            var result = await windowsServiceService.DeleteServiceAsync(parameters);
            return new 
            { 
                success = result.Success, 
                action = "delete_service", 
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
            var windowsServiceService = scope.ServiceProvider.GetRequiredService<WindowsServiceService>();

            var status = await windowsServiceService.GetServiceStatusAsync(parameters);
            return new 
            { 
                success = true, 
                action = "get_service_status", 
                service = status.ServiceName,
                data = status
            };
        }
    }
}