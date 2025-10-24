using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using EvvaAgent.Data;

using EvvaAgent.Infrastructure.Communication;
using EvvaAgent.Infrastructure.Execution;
using EvvaAgent.Infrastructure.Deployment;
using EvvaAgent.Infrastructure.Metrics;
using EvvaAgent.Modules.Nginx.Services;
using EvvaAgent.Workers;
using EvvaAgent.Core.Extensions;
using EvvaAgent.Core.Commands;



var builder = WebApplication.CreateBuilder(args);

// Parse command line arguments

var dbPath = args.Contains("--db") ? args[Array.IndexOf(args, "--db") + 1] : Path.Combine(Environment.CurrentDirectory, "Data", "EvvaAgent.db");
var execMode = args.Contains("--exec-mode") ? args[Array.IndexOf(args, "--exec-mode") + 1] : "run";


// 1. Add services to the container.
builder.Services.AddLogging();
builder.Services.AddSignalR();

    // Ensure directory exists
var dbDirectory = Path.GetDirectoryName(dbPath);
    if (!string.IsNullOrEmpty(dbDirectory) && !Directory.Exists(dbDirectory))
    {
        Directory.CreateDirectory(dbDirectory);
    }

// 2. Configure EF Core with SQLite
builder.Services.AddDbContext<AgentDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// 3. Register custom application services
builder.Services.AddSingleton<ICommandExecutorService, CommandExecutorService>();
builder.Services.AddScoped<IDeploymentService, DeploymentService>();
builder.Services.AddSingleton<IMetricsService, MetricsService>();
builder.Services.AddSingleton<ICoreHubService, CoreHubService>();
builder.Services.AddScoped<NginxService>();
builder.Services.AddEvvaModules();

// 4. Register the background workers
builder.Services.AddHostedService<MetricsCollectorWorker>();


var app = builder.Build();

// --- Configure the HTTP request pipeline ---

// 1. Ensure the database is created
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AgentDbContext>();

    dbContext.Database.Migrate();
}



// 5. Get available modules and commands
app.MapGet("/api/modules", (ModularCommandService commandService) =>
{
    var commands = commandService.GetAvailableCommands();
    return Results.Ok(new { commands });
});



// 3. Configure modules
app.Services.ConfigureEvvaModules();

// 4. Start Nginx service


// 5. Update configuration from command line arguments
if (args.Length > 0)
{
    using var scope = app.Services.CreateScope();
    var configRepo = scope.ServiceProvider.GetRequiredService<EvvaAgent.Domain.Repositories.IConfigurationRepository>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    var config = await configRepo.GetAsync() ?? new EvvaAgent.Domain.Configuration();
    
    if (args.Contains("--admin-server-url"))
        config.AdminServerUrl = args[Array.IndexOf(args, "--admin-server-url") + 1];
    if (args.Contains("--access-token"))
        config.Token = args[Array.IndexOf(args, "--access-token") + 1];
    if(args.Contains("--nginx-path"))
        config.NginxPath = args[Array.IndexOf(args, "--nginx-path") + 1];
    if(args.Contains("--nginx-config-path"))
        config.NginxConfigPath = args[Array.IndexOf(args, "--nginx-config-path") + 1];
    if(args.Contains("--firt-run"))
        config.FirstRun = 1;
    await configRepo.SaveAsync(config);
    logger.LogInformation("Configuration updated from command line arguments");
}

if (execMode != "setup")
{
    using (var scope = app.Services.CreateScope())
    {
        var nginxService = scope.ServiceProvider.GetRequiredService<NginxService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var nginxStatus = await nginxService.GetNginxStatusAsync();
        if (!nginxStatus.IsRunning)
        {
            logger.LogInformation("Attempting to start Nginx service...");
            var nginxStarted = await nginxService.StartNginxAsync();

            if (nginxStarted)
            {
                logger.LogInformation("Nginx service started successfully");
            }
            else
            {
                logger.LogWarning("Failed to start Nginx service - continuing without it");
            }
        }
    }
    
    // Start core connection after configuration is ready (non-blocking)
    var coreHub = app.Services.GetRequiredService<ICoreHubService>();
    _ = Task.Run(async () => await coreHub.StartAsync());
    Console.WriteLine("Starting application...");
    await app.RunAsync();
}
