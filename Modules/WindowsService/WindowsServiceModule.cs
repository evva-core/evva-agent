using EvvaAgent.Core.Abstractions;
using EvvaAgent.Modules.WindowsService.Resources;

namespace EvvaAgent.Modules.WindowsService
{
    public class WindowsServiceModule : IEvvaModule
    {
        public string Name => "winservice";

        public IEnumerable<string> GetAvailableCommands()
        {
            return new[]
            {
                "winservice.create",
                "winservice.start", 
                "winservice.stop",
                "winservice.restart",
                "winservice.delete",
                "winservice.status"
            };
        }

        public async Task<object> ExecuteCommandAsync(string command, string? parameters, IServiceProvider serviceProvider)
        {
            var parts = command.Split('.');
            if (parts.Length < 3 || parts[0] != "evva" || parts[1] != "winservice")
            {
                return new { success = false, error = "Invalid command format" };
            }

            var method = parts[2];
            
            using var scope = serviceProvider.CreateScope();
            var resource = scope.ServiceProvider.GetRequiredService<WindowsServiceResource>();
            
            return await resource.ExecuteAsync(method, parameters, serviceProvider);
        }
    }
}