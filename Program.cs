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
using EvvaAgent.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Configure Services ---

// 1. Add services to the container.
builder.Services.AddLogging();
builder.Services.AddSignalR();

// 2. Configure EF Core with SQLite
builder.Services.AddDbContext<AgentDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=evva-agent.db"));

// 3. Register custom application services
builder.Services.AddSingleton<ICommandExecutorService, CommandExecutorService>();
builder.Services.AddScoped<IDeploymentService, DeploymentService>();
builder.Services.AddSingleton<IMetricsService, MetricsService>();
builder.Services.AddSingleton<ICoreHubService, CoreHubService>();
builder.Services.AddSingleton<EvvaAgent.Services.ServiceManager>();
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

// 2. Map the SignalR Hub


// 3. Add service management endpoints
app.MapPost("/api/service/create", async (ServiceConfig config, ServiceManager serviceManager) =>
{
    var result = await serviceManager.CreateServiceAsync(config);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
});

app.MapPost("/api/service/{serviceName}/start", async (string serviceName, ServiceManager serviceManager) =>
{
    var result = await serviceManager.StartServiceAsync(serviceName);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
});

app.MapPost("/api/service/{serviceName}/stop", async (string serviceName, ServiceManager serviceManager) =>
{
    var result = await serviceManager.StopServiceAsync(serviceName);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
});

app.MapDelete("/api/service/{serviceName}", async (string serviceName, ServiceManager serviceManager) =>
{
    var result = await serviceManager.DeleteServiceAsync(serviceName);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
});

// 3. Configure modules
app.Services.ConfigureEvvaModules();

// 4. Start Nginx service
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
// 5. Start core connection
var coreHub = app.Services.GetRequiredService<ICoreHubService>();
await coreHub.StartAsync();

app.Run();