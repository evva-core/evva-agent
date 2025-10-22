using System.Text.Json;
using EvvaAgent.Core.Abstractions;
using EvvaAgent.Modules.Nginx.Domain;
using EvvaAgent.Modules.Nginx.Services;

namespace EvvaAgent.Modules.Nginx.Resources
{
    public class NginxResource : IEvvaResource
    {
        private readonly Dictionary<string, Func<NginxService, string?, Task<object>>> _methods;
        private readonly JsonSerializerOptions _jsonOptions;

        public NginxResource()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            _methods = new Dictionary<string, Func<NginxService, string?, Task<object>>>
            {
                ["add.server"] = async (service, parameters) =>
                {
                    var config = JsonSerializer.Deserialize<NginxServerConfig>(parameters ?? "{}", _jsonOptions);
                    var result = await service.AddServerAsync(config!);
                    return new { success = result, action = "add_server", serverName = config!.ServerName };
                },
                ["add.proxy"] = async (service, parameters) =>
                {
                    var config = JsonSerializer.Deserialize<ReverseProxyConfig>(parameters ?? "{}", _jsonOptions);
                    var result = await service.AddReverseProxyAsync(config!);
                    return new { success = result, action = "add_proxy", domain = config!.Domain };
                },
                ["add.static"] = async (service, parameters) =>
                {
                    var config = JsonSerializer.Deserialize<StaticSiteConfig>(parameters ?? "{}", _jsonOptions);
                    var result = await service.AddStaticSiteAsync(config!);
                    return new { success = result, action = "add_static", domain = config!.Domain };
                },
                ["remove.server"] = async (service, parameters) =>
                {
                    var result = await service.RemoveServerAsync(parameters ?? "");
                    return new { success = result, action = "remove_server", serverName = parameters };
                },
                ["enable.site"] = async (service, parameters) =>
                {
                    var result = await service.EnableSiteAsync(parameters ?? "");
                    return new { success = result, action = "enable_site", serverName = parameters };
                },
                ["disable.site"] = async (service, parameters) =>
                {
                    var result = await service.DisableSiteAsync(parameters ?? "");
                    return new { success = result, action = "disable_site", serverName = parameters };
                },
                ["test.config"] = async (service, parameters) =>
                {
                    var result = await service.TestNginxConfigurationAsync();
                    return new { success = result, action = "test_config", message = result ? "Configuration test successful" : "Configuration test failed" };
                },
                ["reload"] = async (service, parameters) =>
                {
                    var result = await service.ReloadNginxAsync();
                    return new { success = result, action = "reload", message = result ? "Nginx reloaded successfully" : "Failed to reload Nginx" };
                },
                ["restart"] = async (service, parameters) =>
                {
                    var result = await service.RestartNginxAsync();
                    return new { success = result, action = "restart", message = result ? "Nginx restarted successfully" : "Failed to restart Nginx" };
                },
                ["status"] = async (service, parameters) =>
                {
                    var status = await service.GetNginxStatusAsync();
                    return new { success = true, action = "get_status", data = status };
                }
            };
        }

        public async Task<object> ExecuteAsync(string method, string? parameters, IServiceProvider serviceProvider)
        {
            if (!_methods.ContainsKey(method))
            {
                return new { success = false, error = $"Method '{method}' not found" };
            }

            using var scope = serviceProvider.CreateScope();
            var nginxService = scope.ServiceProvider.GetRequiredService<NginxService>();
            return await _methods[method](nginxService, parameters);
        }

        public IEnumerable<string> GetAvailableMethods()
        {
            return _methods.Keys;
        }
    }
}