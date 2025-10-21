using EvvaAgent.Core.Abstractions;
using EvvaAgent.Core.Commands;
using EvvaAgent.Modules.Nginx;

namespace EvvaAgent.Core.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddEvvaModules(this IServiceCollection services)
        {
            services.AddSingleton<ModularCommandService>();
            
            // Register modules
            services.AddSingleton<IEvvaModule, NginxModule>();
            
            return services;
        }

        public static void ConfigureEvvaModules(this IServiceProvider serviceProvider)
        {
            var commandService = serviceProvider.GetRequiredService<ModularCommandService>();
            var modules = serviceProvider.GetServices<IEvvaModule>();

            foreach (var module in modules)
            {
                commandService.RegisterModule(module);
            }
        }
    }
}