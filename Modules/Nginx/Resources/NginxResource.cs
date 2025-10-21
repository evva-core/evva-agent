using System.Text.Json;
using EvvaAgent.Core.Abstractions;
using EvvaAgent.Modules.Nginx.Domain;
using EvvaAgent.Modules.Nginx.Services;

namespace EvvaAgent.Modules.Nginx.Resources
{
    public class NginxResource : IEvvaResource
    {
        private readonly Dictionary<string, Func<NginxService, string?, Task<object>>> _methods;

        public NginxResource()
        {
            _methods = new Dictionary<string, Func<NginxService, string?, Task<object>>>
            {
                ["add.server"] = async (service, parameters) =>
                {
                    var config = JsonSerializer.Deserialize<NginxServerConfig>(parameters ?? "{}");
                    var result = await service.AddServerAsync(config!);
                    return new { success = result, action = "add_server", serverName = config!.ServerName };
                },
                ["add.proxy"] = async (service, parameters) =>
                {
                    // Handle double-escaped JSON
                    var cleanJson = parameters ?? "{}";
                    if (cleanJson.StartsWith('"') && cleanJson.EndsWith('"'))
                    {
                        cleanJson = JsonSerializer.Deserialize<string>(cleanJson) ?? "{}";
                    }
                    
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    
                    var config = JsonSerializer.Deserialize<ReverseProxyConfig>(cleanJson, options);
                    var serverConfig = new NginxServerConfig
                    {
                        ServerName = config!.Domain,
                        Port = config.Port,
                        Enabled = true,
                        Locations = new List<NginxLocation>
                        {
                            new NginxLocation { Path = "/", ProxyPass = config.TargetUrl }
                        }
                    };
                    var result = await service.AddServerAsync(serverConfig);
                    return new { success = result, action = "add_proxy", domain = config.Domain };
                },
                ["add.static"] = async (service, parameters) =>
                {
                    // Handle double-escaped JSON
                    var cleanJson = parameters ?? "{}";
                    if (cleanJson.StartsWith('"') && cleanJson.EndsWith('"'))
                    {
                        cleanJson = JsonSerializer.Deserialize<string>(cleanJson) ?? "{}";
                    }
                    
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    
                    var config = JsonSerializer.Deserialize<StaticSiteConfig>(cleanJson, options);
                    
                    var serverConfig = new NginxServerConfig
                    {
                        ServerName = config!.Domain,
                        Port = config.Port,
                        Root = config.RootPath,
                        Index = config.IndexFiles ?? "index.html index.htm",
                        Enabled = true,
                        Locations = new List<NginxLocation>
                        {
                            new NginxLocation { Path = "/", TryFiles = "$uri $uri/ =404" }
                        }
                    };
                    var result = await service.AddServerAsync(serverConfig);
                    return new { success = result, action = "add_static", domain = config.Domain };
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