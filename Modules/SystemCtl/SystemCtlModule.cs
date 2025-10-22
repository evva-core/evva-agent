using EvvaAgent.Core.Abstractions;
using EvvaAgent.Modules.SystemCtl.Resources;

namespace EvvaAgent.Modules.SystemCtl
{
    public class SystemCtlModule : IEvvaModule
    {
        public string Name => "systemctl";

        public IEnumerable<string> GetAvailableCommands()
        {
            return new[]
            {
                "systemctl.create",
                "systemctl.start",
                "systemctl.stop", 
                "systemctl.restart",
                "systemctl.status",
                "systemctl.enable",
                "systemctl.disable"
            };
        }

        public async Task<object> ExecuteCommandAsync(string command, string? parameters, IServiceProvider serviceProvider)
        {
            var parts = command.Split('.');
            if (parts.Length < 3 || parts[0] != "evva" || parts[1] != "systemctl")
            {
                return new { success = false, error = "Invalid command format" };
            }

            var method = parts[2];
            
            using var scope = serviceProvider.CreateScope();
            var resource = scope.ServiceProvider.GetRequiredService<SystemCtlResource>();
            
            return await resource.ExecuteAsync(method, parameters, serviceProvider);
        }
    }
}