using EvvaAgent.Core.Abstractions;
using EvvaAgent.Modules.Nginx.Resources;

namespace EvvaAgent.Modules.Nginx
{
    public class NginxModule : IEvvaModule
    {
        public string Name => "nginx";

        public IEnumerable<string> GetAvailableCommands()
        {
            return
            [
                "nginx.add.server",
                "nginx.add.proxy", 
                "nginx.add.static",
                "nginx.remove.server",
                "nginx.enable.site",
                "nginx.disable.site",
                "nginx.test.config",
                "nginx.reload",
                "nginx.restart",
                "nginx.status"
            ];
        }

        public async Task<object> ExecuteCommandAsync(string command, string? parameters, IServiceProvider serviceProvider)
        {
            var parts = command.Split('.');
            if (parts.Length < 3 || parts[0] != "evva" || parts[1] != "nginx")
            {
                return new { success = false, error = "Invalid command format" };
            }

            var method = string.Join(".", parts.Skip(2));
            
            using var scope = serviceProvider.CreateScope();
            var resource = scope.ServiceProvider.GetRequiredService<NginxResource>();
            
            return await resource.ExecuteAsync(method, parameters, serviceProvider);
        }
    }
}