using EvvaAgent.Core.Abstractions;

namespace EvvaAgent.Core.Commands
{
    public class ModularCommandService
    {
        private readonly Dictionary<string, IEvvaModule> _modules = new();
        private readonly ILogger<ModularCommandService> _logger;

        public ModularCommandService(ILogger<ModularCommandService> logger)
        {
            _logger = logger;
        }

        public void RegisterModule(IEvvaModule module)
        {
            _modules[module.Name] = module;
            _logger.LogInformation("Registered module: {ModuleName}", module.Name);
        }

        public async Task<object> ExecuteCommandAsync(string commandKey, string? parameters, IServiceProvider serviceProvider)
        {
            try
            {
                if (!commandKey.StartsWith("evva."))
                {
                    return new { success = false, error = "Command must start with 'evva.'" };
                }

                var parts = commandKey.Split('.');
                if (parts.Length < 3)
                {
                    return new { success = false, error = "Invalid command format" };
                }

                var moduleName = parts[1];
                if (!_modules.ContainsKey(moduleName))
                {
                    return new { success = false, error = $"Module '{moduleName}' not found" };
                }

                return await _modules[moduleName].ExecuteCommandAsync(commandKey, parameters, serviceProvider);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing command: {CommandKey}", commandKey);
                return new { success = false, error = ex.Message };
            }
        }

        public IEnumerable<string> GetAvailableCommands()
        {
            return _modules.Values.SelectMany(m => m.GetAvailableCommands());
        }
    }
}