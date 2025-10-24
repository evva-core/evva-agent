using EvvaAgent.Core.Abstractions;
using EvvaAgent.Core.Commands;
using EvvaAgent.Modules.Nginx;
using EvvaAgent.Domain.Repositories;
using EvvaAgent.Infrastructure.Data;
using EvvaAgent.Modules.Nginx.Services;
using EvvaAgent.Modules.Nginx.Resources;
using EvvaAgent.Modules.SystemCtl;
using EvvaAgent.Modules.SystemCtl.Services;
using EvvaAgent.Modules.SystemCtl.Resources;
using EvvaAgent.Modules.WindowsService;
using EvvaAgent.Modules.WindowsService.Services;
using EvvaAgent.Modules.WindowsService.Resources;

namespace EvvaAgent.Core.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddEvvaModules(this IServiceCollection services)
        {
            services.AddSingleton<ModularCommandService>();
            
            // Register Nginx Module
            services.AddSingleton<IEvvaModule, NginxModule>();
            services.AddScoped<NginxService>();
            services.AddScoped<NginxResource>();
            
            // Register SystemCtl Module
            services.AddSingleton<IEvvaModule, SystemCtlModule>();
            services.AddScoped<SystemCtlService>();
            services.AddScoped<SystemCtlResource>();
            
            // Register WindowsService Module
            services.AddSingleton<IEvvaModule, WindowsServiceModule>();
            services.AddScoped<WindowsServiceService>();
            services.AddScoped<WindowsServiceResource>();
            
            // Register repositories
            services.AddScoped<ICollectMetricRepository, CollectMetricRepository>();
            services.AddScoped<IConfigurationRepository, ConfigurationRepository>();
            services.AddScoped<IInformationRepository, InformationRepository>();
            services.AddScoped<ILogRepository, LogRepository>();
            services.AddScoped<IProjectRepository, ProjectRepository>();
            
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