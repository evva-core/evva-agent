using EvvaAgent.Core.Abstractions;
using EvvaAgent.Modules.Nginx.Resources;

namespace EvvaAgent.Modules.Nginx
{
    public class NginxModule : IEvvaModule
    {
        public string Name => "nginx";
        private readonly NginxResource _resource = new();

        public IEnumerable<string> GetAvailableCommands()
        {
            return _resource.GetAvailableMethods().Select(m => $"{Name}.{m}");
        }

        public async Task<object> ExecuteCommandAsync(string command, string? parameters, IServiceProvider serviceProvider)
        {
            var method = command.Replace($"evva.{Name}.", "");
            return await _resource.ExecuteAsync(method, parameters, serviceProvider);
        }
    }
}