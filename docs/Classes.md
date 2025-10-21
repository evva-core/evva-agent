# EvvaAgent - Complete API Reference

## Core Classes

### ModularCommandService
**Namespace**: `EvvaAgent.Core.Commands`

Central orchestrator for the modular command system.

```csharp
public class ModularCommandService
{
    public void RegisterModule(IEvvaModule module)
    public async Task<object> ExecuteCommandAsync(string commandKey, string? parameters, IServiceProvider serviceProvider)
    public IEnumerable<string> GetAvailableCommands()
    public bool IsCommandAvailable(string command)
    public IEvvaModule? GetModule(string moduleName)
}
```

**Key Methods**:
- `RegisterModule()`: Registers a new module with the command system
- `ExecuteCommandAsync()`: Routes and executes commands to appropriate modules
- `GetAvailableCommands()`: Returns all available commands across modules
- `IsCommandAvailable()`: Validates if a command exists
- `GetModule()`: Retrieves a specific module by name

### ServiceCollectionExtensions
**Namespace**: `EvvaAgent.Core.Extensions`

Dependency injection configuration extensions.

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEvvaModules(this IServiceCollection services)
    public static IServiceProvider ConfigureEvvaModules(this IServiceProvider serviceProvider)
}
```

## Core Interfaces

### IEvvaModule
**Namespace**: `EvvaAgent.Core.Abstractions`

Base interface for all system modules.

```csharp
public interface IEvvaModule
{
    string Name { get; }
    IEnumerable<string> GetAvailableCommands();
    Task<object> ExecuteCommandAsync(string command, string? parameters, IServiceProvider serviceProvider);
}
```

### IEvvaResource
**Namespace**: `EvvaAgent.Core.Abstractions`

Interface for command resource implementations.

```csharp
public interface IEvvaResource
{
    Task<object> ExecuteAsync(string method, string? parameters, IServiceProvider serviceProvider);
    IEnumerable<string> GetAvailableMethods();
}
```

## Nginx Module Classes

### NginxModule
**Namespace**: `EvvaAgent.Modules.Nginx`

Main Nginx module implementation.

```csharp
public class NginxModule : IEvvaModule
{
    public string Name => "nginx";
    public IEnumerable<string> GetAvailableCommands()
    public async Task<object> ExecuteCommandAsync(string command, string? parameters, IServiceProvider serviceProvider)
}
```

### NginxService
**Namespace**: `EvvaAgent.Modules.Nginx.Services`

Core Nginx business logic service.

```csharp
public class NginxService
{
    // Server Management
    public async Task<bool> AddServerAsync(NginxServerConfig serverConfig)
    public async Task<bool> RemoveServerAsync(string serverName)
    public async Task<bool> EnableSiteAsync(string serverName)
    public async Task<bool> DisableSiteAsync(string serverName)
    
    // Configuration Management
    public async Task<bool> TestNginxConfigurationAsync()
    public async Task<bool> ReloadNginxAsync()
    public async Task<bool> StartNginxAsync()
    public async Task<bool> RestartNginxAsync()
    
    // Status and Monitoring
    public async Task<NginxStatus> GetNginxStatusAsync()
    
    // Quick Configuration Methods
    public async Task<bool> AddReverseProxyAsync(ReverseProxyConfig config)
    public async Task<bool> AddStaticSiteAsync(StaticSiteConfig config)
}
```

### NginxResource
**Namespace**: `EvvaAgent.Modules.Nginx.Resources`

Nginx command resource implementation.

```csharp
public class NginxResource : IEvvaResource
{
    public async Task<object> ExecuteAsync(string method, string? parameters, IServiceProvider serviceProvider)
    public IEnumerable<string> GetAvailableMethods()
}
```

**Available Methods**:
- `add.server` - Add server configuration
- `add.proxy` - Add reverse proxy
- `add.static` - Add static site
- `remove.server` - Remove server
- `enable.site` - Enable site
- `disable.site` - Disable site
- `test.config` - Test configuration
- `reload` - Reload Nginx
- `restart` - Restart Nginx
- `status` - Get status

## Nginx Domain Models

### NginxServerConfig
**Namespace**: `EvvaAgent.Modules.Nginx.Domain`

Complete server configuration model.

```csharp
public class NginxServerConfig
{
    public string ServerName { get; set; } = string.Empty;
    public int Port { get; set; } = 80;
    public string Root { get; set; } = string.Empty;
    public string Index { get; set; } = "index.html index.htm";
    public bool Enabled { get; set; } = true;
    public List<NginxLocation> Locations { get; set; } = new();
    public List<string> CustomDirectives { get; set; } = new();
}
```

### NginxLocation
**Namespace**: `EvvaAgent.Modules.Nginx.Domain`

Location block configuration.

```csharp
public class NginxLocation
{
    public string Path { get; set; } = "/";
    public string ProxyPass { get; set; } = string.Empty;
    public string TryFiles { get; set; } = string.Empty;
    public List<string> CustomDirectives { get; set; } = new();
}
```

### NginxStatus
**Namespace**: `EvvaAgent.Modules.Nginx.Domain`

Nginx status information.

```csharp
public class NginxStatus
{
    public bool IsRunning { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public int ProcessCount { get; set; }
    public List<NginxProcessInfo> Processes { get; set; } = new();
}
```

### NginxProcessInfo
**Namespace**: `EvvaAgent.Modules.Nginx.Domain`

Individual Nginx process information.

```csharp
public class NginxProcessInfo
{
    public int Id { get; set; }
    public DateTime StartTime { get; set; }
    public long WorkingSet { get; set; }
    public string ProcessName { get; set; } = string.Empty;
}
```

### ReverseProxyConfig
**Namespace**: `EvvaAgent.Modules.Nginx.Domain`

Reverse proxy configuration model.

```csharp
public class ReverseProxyConfig
{
    public string Domain { get; set; } = string.Empty;
    public int Port { get; set; } = 80;
    public string TargetUrl { get; set; } = string.Empty;
    public bool EnableSSL { get; set; } = false;
    public Dictionary<string, string> Headers { get; set; } = new();
}
```

### StaticSiteConfig
**Namespace**: `EvvaAgent.Modules.Nginx.Domain`

Static site configuration model.

```csharp
public class StaticSiteConfig
{
    public string Domain { get; set; } = string.Empty;
    public int Port { get; set; } = 80;
    public string RootPath { get; set; } = string.Empty;
    public string? IndexFiles { get; set; } = "index.html index.htm";
    public bool EnableDirectoryListing { get; set; } = false;
    public Dictionary<string, string> CustomHeaders { get; set; } = new();
}
```

## Infrastructure Classes

### CoreHubService
**Namespace**: `EvvaAgent.Infrastructure.Communication`

SignalR communication service with EvvaCore.

```csharp
public class CoreHubService : ICoreHubService
{
    public bool IsConnected { get; }
    public event EventHandler<string>? CommandReceived;
    public event EventHandler<bool>? ConnectionStateChanged;
    
    public async Task<bool> SendMetricsAsync(object metrics)
    public async Task<bool> SendLogAsync(string level, string message, object? data = null)
    public async Task StartAsync()
    public async Task StopAsync()
    public async Task NotifyDeploymentProgress(int projectId, string stage, string message, bool isError = false)
    public async Task NotifyDeploymentCompleted(int projectId, bool success, string message)
}
```

### CommandExecutorService
**Namespace**: `EvvaAgent.Infrastructure.Execution`

Cross-platform command execution service.

```csharp
public class CommandExecutorService : ICommandExecutorService
{
    public async Task<CommandResult> ExecuteCommandAsync(string command)
    public async Task<CommandResult> ExecuteCommandAsync(string command, string workingDirectory)
    public async Task<CommandResult> ExecuteCommandAsync(string command, string workingDirectory, Dictionary<string, string> environment)
    public async Task<CommandResult> ExecuteCommandWithTimeoutAsync(string command, TimeSpan timeout)
    public async Task<bool> IsCommandAvailableAsync(string command)
}
```

### CommandResult
**Namespace**: `EvvaAgent.Infrastructure.Execution`

Command execution result.

```csharp
public class CommandResult
{
    public string Output { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public int ExitCode { get; set; }
    public TimeSpan Duration { get; set; }
    public bool Success => ExitCode == 0;
}
```

### DeploymentService
**Namespace**: `EvvaAgent.Infrastructure.Deployment`

Deployment orchestration service.

```csharp
public class DeploymentService : IDeploymentService
{
    public async Task<DeploymentResult> DeployProjectAsync(DeploymentRequest request)
    public async Task<DeploymentStatus> GetDeploymentStatusAsync(int deploymentId)
    public async Task<bool> CancelDeploymentAsync(int deploymentId)
    public async Task<IEnumerable<DeploymentHistory>> GetDeploymentHistoryAsync(int projectId)
}
```

### MetricsService
**Namespace**: `EvvaAgent.Infrastructure.Metrics`

System metrics collection service.

```csharp
public class MetricsService : IMetricsService
{
    public async Task<HostMetricsDto> GetSystemMetricsAsync()
    public async Task<ServiceMetrics> GetServiceMetricsAsync(string serviceName)
    public async Task RecordCustomMetricAsync(string name, object value, Dictionary<string, string>? tags = null)
    public async Task<IEnumerable<MetricHistory>> GetMetricHistoryAsync(string metricName, TimeSpan period)
}
```

## Domain Entities

### Project
**Namespace**: `EvvaAgent.Domain`

Project entity for deployments.

```csharp
public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string RepositoryUrl { get; set; } = string.Empty;
    public string Branch { get; set; } = "main";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastDeployedAt { get; set; }
    public string? LastDeploymentStatus { get; set; }
}
```

### Configuration
**Namespace**: `EvvaAgent.Domain`

System configuration entity.

```csharp
public class Configuration
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = "General";
    public bool IsEncrypted { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

### Log
**Namespace**: `EvvaAgent.Domain`

Audit and operational logging entity.

```csharp
public class Log
{
    public int Id { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
    public string? Source { get; set; }
    public DateTime Timestamp { get; set; }
    public string? CorrelationId { get; set; }
    public string? AdditionalData { get; set; }
}
```

### CollectMetric
**Namespace**: `EvvaAgent.Domain`

Metrics collection entity.

```csharp
public class CollectMetric
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Value { get; set; }
    public string? Unit { get; set; }
    public string? Tags { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Source { get; set; }
}
```

### Information
**Namespace**: `EvvaAgent.Domain`

System information entity.

```csharp
public class Information
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Category { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

## DTOs and Data Transfer Objects

### HostMetricsDto
**Namespace**: `EvvaAgent.DTOs`

System metrics data transfer object.

```csharp
public class HostMetricsDto
{
    public double CpuUsage { get; set; }
    public long MemoryUsed { get; set; }
    public long MemoryTotal { get; set; }
    public long DiskUsed { get; set; }
    public long DiskTotal { get; set; }
    public double NetworkIn { get; set; }
    public double NetworkOut { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> AdditionalMetrics { get; set; } = new();
}
```

### DeploymentRequest
**Namespace**: `EvvaAgent.Domain`

Deployment request model.

```csharp
public class DeploymentRequest
{
    public int ProjectId { get; set; }
    public List<Repository> Repositories { get; set; } = new();
    public List<WorkflowStep> WorkflowSteps { get; set; } = new();
    public Dictionary<string, string> Environment { get; set; } = new();
    public string? Description { get; set; }
}
```

### Repository
**Namespace**: `EvvaAgent.Domain`

Repository information for deployments.

```csharp
public class Repository
{
    public string Url { get; set; } = string.Empty;
    public string Branch { get; set; } = "main";
    public string? Tag { get; set; }
    public string? CommitHash { get; set; }
    public string LocalPath { get; set; } = string.Empty;
    public Dictionary<string, string> Credentials { get; set; } = new();
}
```

### WorkflowStep
**Namespace**: `EvvaAgent.Domain`

Individual workflow step definition.

```csharp
public class WorkflowStep
{
    public string WorkflowName { get; set; } = string.Empty;
    public string StageName { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public string? Parameters { get; set; }
    public string? Description { get; set; }
    public int ExecutionOrder { get; set; }
    public bool ContinueOnError { get; set; } = false;
    public TimeSpan? Timeout { get; set; }
}
```

### DeploymentResult
**Namespace**: `EvvaAgent.Infrastructure.Deployment`

Deployment execution result.

```csharp
public class DeploymentResult
{
    public int DeploymentId { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public List<StepResult> StepResults { get; set; } = new();
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
```

### StepResult
**Namespace**: `EvvaAgent.Infrastructure.Deployment`

Individual step execution result.

```csharp
public class StepResult
{
    public string StepName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Output { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
```

## Repository Interfaces

### IProjectRepository
**Namespace**: `EvvaAgent.Domain.Repositories`

Project data access interface.

```csharp
public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(int id);
    Task<IEnumerable<Project>> GetAllAsync();
    Task<IEnumerable<Project>> GetActiveProjectsAsync();
    Task<Project?> GetByNameAsync(string name);
    Task<Project> CreateAsync(Project project);
    Task UpdateAsync(Project project);
    Task DeleteAsync(int id);
}
```

### IConfigurationRepository
**Namespace**: `EvvaAgent.Domain.Repositories`

Configuration data access interface.

```csharp
public interface IConfigurationRepository
{
    Task<Configuration?> GetByKeyAsync(string key);
    Task<IEnumerable<Configuration>> GetByCategoryAsync(string category);
    Task<Configuration> SetAsync(string key, string value, string? category = null);
    Task<bool> DeleteAsync(string key);
    Task<IEnumerable<Configuration>> GetAllAsync();
}
```

### ILogRepository
**Namespace**: `EvvaAgent.Domain.Repositories`

Logging data access interface.

```csharp
public interface ILogRepository
{
    Task<Log> CreateAsync(Log log);
    Task<IEnumerable<Log>> GetByLevelAsync(string level, int limit = 100);
    Task<IEnumerable<Log>> GetByTimeRangeAsync(DateTime from, DateTime to);
    Task<IEnumerable<Log>> GetBySourceAsync(string source, int limit = 100);
    Task CleanupOldLogsAsync(TimeSpan retention);
}
```

### ICollectMetricRepository
**Namespace**: `EvvaAgent.Domain.Repositories`

Metrics data access interface.

```csharp
public interface ICollectMetricRepository
{
    Task<CollectMetric> CreateAsync(CollectMetric metric);
    Task<IEnumerable<CollectMetric>> GetByNameAsync(string name, TimeSpan period);
    Task<IEnumerable<CollectMetric>> GetByTimeRangeAsync(DateTime from, DateTime to);
    Task<double> GetAverageAsync(string name, TimeSpan period);
    Task CleanupOldMetricsAsync(TimeSpan retention);
}
```

## Service Manager (Legacy)

### ServiceManager
**Namespace**: `EvvaAgent.Services`

Legacy service management functionality.

```csharp
public class ServiceManager
{
    public async Task<ServiceResult> CreateServiceAsync(ServiceConfig config)
    public async Task<ServiceResult> StartServiceAsync(string serviceName)
    public async Task<ServiceResult> StopServiceAsync(string serviceName)
    public async Task<ServiceResult> DeleteServiceAsync(string serviceName)
    public async Task<ServiceStatus> GetServiceStatusAsync(string serviceName)
    public async Task<IEnumerable<ServiceInfo>> GetAllServicesAsync()
}
```

### ServiceConfig
**Namespace**: `EvvaAgent.Services`

Service configuration model.

```csharp
public class ServiceConfig
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public string? Arguments { get; set; }
    public string? WorkingDirectory { get; set; }
    public ServiceStartType StartType { get; set; } = ServiceStartType.Manual;
    public Dictionary<string, string> Environment { get; set; } = new();
}
```

### ServiceResult
**Namespace**: `EvvaAgent.Services`

Service operation result.

```csharp
public class ServiceResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public object? Data { get; set; }
}
```

## Background Workers

### MetricsCollectorWorker
**Namespace**: `EvvaAgent.Workers`

Background service for metrics collection.

```csharp
public class MetricsCollectorWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    public async Task CollectAndSendMetricsAsync()
}
```

## Database Context

### AgentDbContext
**Namespace**: `EvvaAgent.Data`

Entity Framework database context.

```csharp
public class AgentDbContext : DbContext
{
    public DbSet<Project> Projects { get; set; }
    public DbSet<Configuration> Configurations { get; set; }
    public DbSet<Log> Logs { get; set; }
    public DbSet<CollectMetric> CollectMetrics { get; set; }
    public DbSet<Information> Information { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
}
```

## Usage Examples

### Module Command Execution
```csharp
// Execute Nginx command
var commandService = serviceProvider.GetRequiredService<ModularCommandService>();
var result = await commandService.ExecuteCommandAsync(
    "evva.nginx.add.proxy", 
    JsonSerializer.Serialize(new ReverseProxyConfig 
    { 
        Domain = "api.example.com", 
        TargetUrl = "http://localhost:5000" 
    })
);
```

### Direct Service Usage
```csharp
// Use Nginx service directly
var nginxService = serviceProvider.GetRequiredService<NginxService>();
var config = new NginxServerConfig
{
    ServerName = "example.com",
    Port = 80,
    Root = "/var/www/html"
};
var success = await nginxService.AddServerAsync(config);
```

### Metrics Collection
```csharp
// Collect system metrics
var metricsService = serviceProvider.GetRequiredService<IMetricsService>();
var metrics = await metricsService.GetSystemMetricsAsync();
Console.WriteLine($"CPU Usage: {metrics.CpuUsage}%");
```

### Deployment Execution
```csharp
// Execute deployment
var deploymentService = serviceProvider.GetRequiredService<IDeploymentService>();
var request = new DeploymentRequest
{
    ProjectId = 1,
    Repositories = new List<Repository> { /* ... */ },
    WorkflowSteps = new List<WorkflowStep> { /* ... */ }
};
var result = await deploymentService.DeployProjectAsync(request);
```

This comprehensive API reference covers all major classes, interfaces, and models in the EvvaAgent system, providing developers with the information needed to understand, extend, and integrate with the system.