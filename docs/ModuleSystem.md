# EvvaAgent Module System

## Overview

The EvvaAgent module system provides a flexible, extensible architecture for adding new functionality through self-contained modules. Each module represents a bounded context with its own domain logic, services, and command implementations.

## Core Interfaces

### IEvvaModule
Base interface that all modules must implement.

```csharp
public interface IEvvaModule
{
    string Name { get; }
    IEnumerable<string> GetAvailableCommands();
    Task<object> ExecuteCommandAsync(string command, string? parameters, IServiceProvider serviceProvider);
}
```

**Properties**:
- `Name`: Unique module identifier (e.g., "nginx", "docker")

**Methods**:
- `GetAvailableCommands()`: Returns list of supported commands
- `ExecuteCommandAsync()`: Routes and executes commands

### IEvvaResource
Interface for command resource implementations within modules.

```csharp
public interface IEvvaResource
{
    Task<object> ExecuteAsync(string method, string? parameters, IServiceProvider serviceProvider);
    IEnumerable<string> GetAvailableMethods();
}
```

**Methods**:
- `ExecuteAsync()`: Executes specific method with parameters
- `GetAvailableMethods()`: Returns supported methods

## Module Architecture

### Standard Module Structure
```
Modules/MyModule/
├── Domain/                    # 📋 Domain Models
│   ├── MyModuleConfig.cs     # Configuration models
│   ├── MyModuleStatus.cs     # Status models
│   └── MyModuleRequest.cs    # Request/Response DTOs
├── Services/                  # 🔧 Business Logic
│   └── MyModuleService.cs    # Core service implementation
├── Resources/                 # 🎯 Command Handlers
│   └── MyModuleResource.cs   # Command implementations
└── MyModule.cs               # 🔌 Module Registration
```

### Module Implementation Example

#### 1. Domain Models
```csharp
// Domain/DockerConfig.cs
namespace EvvaAgent.Modules.Docker.Domain
{
    public class DockerContainerConfig
    {
        public string Name { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public List<string> Ports { get; set; } = new();
        public Dictionary<string, string> Environment { get; set; } = new();
        public List<string> Volumes { get; set; } = new();
        public bool AutoRestart { get; set; } = true;
    }

    public class DockerStatus
    {
        public bool IsRunning { get; set; }
        public string ContainerId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public Dictionary<string, object> Stats { get; set; } = new();
    }
}
```

#### 2. Service Implementation
```csharp
// Services/DockerService.cs
namespace EvvaAgent.Modules.Docker.Services
{
    public class DockerService
    {
        private readonly ILogger<DockerService> _logger;
        private readonly ICommandExecutorService _commandExecutor;

        public DockerService(ILogger<DockerService> logger, ICommandExecutorService commandExecutor)
        {
            _logger = logger;
            _commandExecutor = commandExecutor;
        }

        public async Task<bool> CreateContainerAsync(DockerContainerConfig config)
        {
            try
            {
                _logger.LogInformation("Creating Docker container: {ContainerName}", config.Name);

                var command = BuildDockerRunCommand(config);
                var result = await _commandExecutor.ExecuteCommandAsync(command);

                if (result.ExitCode == 0)
                {
                    _logger.LogInformation("Container {ContainerName} created successfully", config.Name);
                    return true;
                }

                _logger.LogError("Failed to create container {ContainerName}: {Error}", config.Name, result.Error);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating container {ContainerName}", config.Name);
                return false;
            }
        }

        public async Task<DockerStatus> GetContainerStatusAsync(string containerName)
        {
            try
            {
                var command = $"docker inspect {containerName} --format='{{{{json .}}}}'";
                var result = await _commandExecutor.ExecuteCommandAsync(command);

                if (result.ExitCode == 0)
                {
                    // Parse JSON response and return status
                    return ParseDockerInspectOutput(result.Output);
                }

                return new DockerStatus { IsRunning = false, Status = "Not Found" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting container status for {ContainerName}", containerName);
                return new DockerStatus { IsRunning = false, Status = "Error" };
            }
        }

        public async Task<bool> StartContainerAsync(string containerName)
        {
            var result = await _commandExecutor.ExecuteCommandAsync($"docker start {containerName}");
            return result.ExitCode == 0;
        }

        public async Task<bool> StopContainerAsync(string containerName)
        {
            var result = await _commandExecutor.ExecuteCommandAsync($"docker stop {containerName}");
            return result.ExitCode == 0;
        }

        private string BuildDockerRunCommand(DockerContainerConfig config)
        {
            var cmd = new StringBuilder($"docker run -d --name {config.Name}");

            // Add ports
            foreach (var port in config.Ports)
            {
                cmd.Append($" -p {port}");
            }

            // Add environment variables
            foreach (var env in config.Environment)
            {
                cmd.Append($" -e {env.Key}={env.Value}");
            }

            // Add volumes
            foreach (var volume in config.Volumes)
            {
                cmd.Append($" -v {volume}");
            }

            // Add restart policy
            if (config.AutoRestart)
            {
                cmd.Append(" --restart unless-stopped");
            }

            cmd.Append($" {config.Image}");
            return cmd.ToString();
        }

        private DockerStatus ParseDockerInspectOutput(string json)
        {
            // Implementation to parse Docker inspect JSON
            // Return parsed DockerStatus object
            return new DockerStatus();
        }
    }
}
```

#### 3. Resource Implementation
```csharp
// Resources/DockerResource.cs
namespace EvvaAgent.Modules.Docker.Resources
{
    public class DockerResource : IEvvaResource
    {
        private readonly Dictionary<string, Func<string?, IServiceProvider, Task<object>>> _methods;

        public DockerResource()
        {
            _methods = new Dictionary<string, Func<string?, IServiceProvider, Task<object>>>
            {
                ["create"] = ExecuteCreateAsync,
                ["start"] = ExecuteStartAsync,
                ["stop"] = ExecuteStopAsync,
                ["status"] = ExecuteStatusAsync,
                ["remove"] = ExecuteRemoveAsync,
                ["logs"] = ExecuteLogsAsync
            };
        }

        public async Task<object> ExecuteAsync(string method, string? parameters, IServiceProvider serviceProvider)
        {
            if (!_methods.ContainsKey(method))
            {
                return new { success = false, error = $"Method '{method}' not found" };
            }

            try
            {
                return await _methods[method](parameters, serviceProvider);
            }
            catch (Exception ex)
            {
                return new { success = false, error = ex.Message };
            }
        }

        public IEnumerable<string> GetAvailableMethods()
        {
            return _methods.Keys;
        }

        private async Task<object> ExecuteCreateAsync(string? parameters, IServiceProvider serviceProvider)
        {
            if (string.IsNullOrEmpty(parameters))
            {
                return new { success = false, error = "Container configuration required" };
            }

            using var scope = serviceProvider.CreateScope();
            var dockerService = scope.ServiceProvider.GetRequiredService<DockerService>();

            var config = JsonSerializer.Deserialize<DockerContainerConfig>(parameters);
            if (config == null)
            {
                return new { success = false, error = "Invalid configuration format" };
            }

            var result = await dockerService.CreateContainerAsync(config);
            return new { success = result, action = "create_container", container = config.Name };
        }

        private async Task<object> ExecuteStartAsync(string? parameters, IServiceProvider serviceProvider)
        {
            if (string.IsNullOrEmpty(parameters))
            {
                return new { success = false, error = "Container name required" };
            }

            using var scope = serviceProvider.CreateScope();
            var dockerService = scope.ServiceProvider.GetRequiredService<DockerService>();

            var result = await dockerService.StartContainerAsync(parameters);
            return new { success = result, action = "start_container", container = parameters };
        }

        private async Task<object> ExecuteStopAsync(string? parameters, IServiceProvider serviceProvider)
        {
            if (string.IsNullOrEmpty(parameters))
            {
                return new { success = false, error = "Container name required" };
            }

            using var scope = serviceProvider.CreateScope();
            var dockerService = scope.ServiceProvider.GetRequiredService<DockerService>();

            var result = await dockerService.StopContainerAsync(parameters);
            return new { success = result, action = "stop_container", container = parameters };
        }

        private async Task<object> ExecuteStatusAsync(string? parameters, IServiceProvider serviceProvider)
        {
            if (string.IsNullOrEmpty(parameters))
            {
                return new { success = false, error = "Container name required" };
            }

            using var scope = serviceProvider.CreateScope();
            var dockerService = scope.ServiceProvider.GetRequiredService<DockerService>();

            var status = await dockerService.GetContainerStatusAsync(parameters);
            return new { success = true, action = "get_status", container = parameters, status };
        }

        private async Task<object> ExecuteRemoveAsync(string? parameters, IServiceProvider serviceProvider)
        {
            // Implementation for container removal
            return new { success = true, action = "remove_container" };
        }

        private async Task<object> ExecuteLogsAsync(string? parameters, IServiceProvider serviceProvider)
        {
            // Implementation for getting container logs
            return new { success = true, action = "get_logs" };
        }
    }
}
```

#### 4. Module Registration
```csharp
// DockerModule.cs
namespace EvvaAgent.Modules.Docker
{
    public class DockerModule : IEvvaModule
    {
        public string Name => "docker";

        public IEnumerable<string> GetAvailableCommands()
        {
            return new[]
            {
                "docker.create",
                "docker.start",
                "docker.stop",
                "docker.status",
                "docker.remove",
                "docker.logs"
            };
        }

        public async Task<object> ExecuteCommandAsync(string command, string? parameters, IServiceProvider serviceProvider)
        {
            // Parse command: evva.docker.{method}
            var parts = command.Split('.');
            if (parts.Length < 3 || parts[0] != "evva" || parts[1] != "docker")
            {
                return new { success = false, error = "Invalid command format" };
            }

            var method = parts[2];
            
            using var scope = serviceProvider.CreateScope();
            var resource = scope.ServiceProvider.GetRequiredService<DockerResource>();
            
            return await resource.ExecuteAsync(method, parameters, serviceProvider);
        }
    }
}
```

## Module Registration

### 1. Dependency Injection Setup
```csharp
// Core/Extensions/ServiceCollectionExtensions.cs
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEvvaModules(this IServiceCollection services)
    {
        // Register Nginx Module
        services.AddSingleton<IEvvaModule, NginxModule>();
        services.AddScoped<NginxService>();
        services.AddScoped<NginxResource>();

        // Register Docker Module
        services.AddSingleton<IEvvaModule, DockerModule>();
        services.AddScoped<DockerService>();
        services.AddScoped<DockerResource>();

        // Register other modules...

        return services;
    }

    public static IServiceProvider ConfigureEvvaModules(this IServiceProvider serviceProvider)
    {
        var commandService = serviceProvider.GetRequiredService<ModularCommandService>();
        var modules = serviceProvider.GetServices<IEvvaModule>();

        foreach (var module in modules)
        {
            commandService.RegisterModule(module);
        }

        return serviceProvider;
    }
}
```

### 2. Program.cs Integration
```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Register modules
builder.Services.AddEvvaModules();

var app = builder.Build();

// Configure modules
app.Services.ConfigureEvvaModules();

app.Run();
```

## Command Patterns

### Command Format
```
evva.{module}.{action}.{subaction}
```

### Examples
- `evva.docker.create` - Create container
- `evva.docker.start` - Start container
- `evva.nginx.add.proxy` - Add reverse proxy
- `evva.systemctl.restart` - Restart service

### Parameter Formats

#### Simple Parameters
```json
{
  "command": "evva.docker.start",
  "parameters": "my-container"
}
```

#### Complex Parameters
```json
{
  "command": "evva.docker.create",
  "parameters": {
    "name": "web-app",
    "image": "nginx:latest",
    "ports": ["80:80", "443:443"],
    "environment": {
      "ENV": "production"
    },
    "volumes": ["/host/path:/container/path"]
  }
}
```

## Response Standardization

### Success Response
```json
{
  "success": true,
  "action": "create_container",
  "data": {
    "container": "web-app",
    "id": "abc123",
    "status": "running"
  }
}
```

### Error Response
```json
{
  "success": false,
  "error": "Container name already exists",
  "code": "DUPLICATE_NAME"
}
```

## Advanced Module Features

### Configuration Integration
```csharp
public class DockerService
{
    private readonly DockerConfig _config;

    public DockerService(IConfiguration configuration)
    {
        _config = configuration.GetSection("Docker").Get<DockerConfig>() ?? new DockerConfig();
    }
}

public class DockerConfig
{
    public string DockerHost { get; set; } = "unix:///var/run/docker.sock";
    public string DefaultRegistry { get; set; } = "docker.io";
    public int TimeoutSeconds { get; set; } = 30;
}
```

### Health Checks
```csharp
public class DockerHealthCheck : IHealthCheck
{
    private readonly DockerService _dockerService;

    public DockerHealthCheck(DockerService dockerService)
    {
        _dockerService = dockerService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var isHealthy = await _dockerService.IsDockerAvailableAsync();
            return isHealthy ? HealthCheckResult.Healthy("Docker is available") 
                            : HealthCheckResult.Unhealthy("Docker is not available");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Docker health check failed", ex);
        }
    }
}
```

### Metrics Integration
```csharp
public class DockerService
{
    private readonly IMetricsService _metricsService;

    public async Task<bool> CreateContainerAsync(DockerContainerConfig config)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await CreateContainerInternalAsync(config);
            
            await _metricsService.RecordMetricAsync("docker.container.create", new
            {
                success = result,
                duration = stopwatch.ElapsedMilliseconds,
                container = config.Name
            });

            return result;
        }
        finally
        {
            stopwatch.Stop();
        }
    }
}
```

## Testing Modules

### Unit Tests
```csharp
[Test]
public async Task CreateContainer_ValidConfig_ReturnsSuccess()
{
    // Arrange
    var mockCommandExecutor = new Mock<ICommandExecutorService>();
    mockCommandExecutor.Setup(x => x.ExecuteCommandAsync(It.IsAny<string>()))
                      .ReturnsAsync(new CommandResult { ExitCode = 0 });

    var service = new DockerService(Mock.Of<ILogger<DockerService>>(), mockCommandExecutor.Object);
    var config = new DockerContainerConfig { Name = "test", Image = "nginx" };

    // Act
    var result = await service.CreateContainerAsync(config);

    // Assert
    Assert.IsTrue(result);
    mockCommandExecutor.Verify(x => x.ExecuteCommandAsync(It.Is<string>(cmd => cmd.Contains("docker run"))), Times.Once);
}
```

### Integration Tests
```csharp
[Test]
public async Task DockerModule_ExecuteCommand_ReturnsExpectedResult()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddSingleton<IEvvaModule, DockerModule>();
    services.AddScoped<DockerService>();
    services.AddScoped<DockerResource>();
    services.AddScoped<ICommandExecutorService, CommandExecutorService>();

    var serviceProvider = services.BuildServiceProvider();
    var module = serviceProvider.GetRequiredService<IEvvaModule>();

    // Act
    var result = await module.ExecuteCommandAsync("evva.docker.status", "test-container", serviceProvider);

    // Assert
    Assert.IsNotNull(result);
    // Additional assertions...
}
```

## Best Practices

### 1. Error Handling
- Always wrap operations in try-catch blocks
- Return standardized error responses
- Log errors with appropriate context
- Implement graceful degradation

### 2. Logging
- Use structured logging with context
- Include correlation IDs for tracing
- Log at appropriate levels (Debug, Info, Warning, Error)
- Avoid logging sensitive information

### 3. Performance
- Use async/await patterns consistently
- Implement proper resource disposal
- Consider caching for frequently accessed data
- Monitor and optimize slow operations

### 4. Security
- Validate all input parameters
- Sanitize command parameters
- Implement proper authorization
- Audit sensitive operations

### 5. Configuration
- Use strongly-typed configuration classes
- Provide sensible defaults
- Support environment-specific settings
- Document all configuration options

## Module Discovery

### Available Commands API
```csharp
[HttpGet("/api/modules")]
public IActionResult GetModules()
{
    var modules = _serviceProvider.GetServices<IEvvaModule>();
    var result = modules.Select(m => new
    {
        name = m.Name,
        commands = m.GetAvailableCommands()
    });

    return Ok(result);
}
```

### Command Validation
```csharp
public class CommandValidator
{
    private readonly IEnumerable<IEvvaModule> _modules;

    public bool IsValidCommand(string command)
    {
        var parts = command.Split('.');
        if (parts.Length < 3 || parts[0] != "evva")
            return false;

        var moduleName = parts[1];
        var module = _modules.FirstOrDefault(m => m.Name == moduleName);
        
        return module?.GetAvailableCommands().Contains(command) ?? false;
    }
}
```

This comprehensive module system provides a robust foundation for extending EvvaAgent with new capabilities while maintaining clean architecture principles and ensuring consistent behavior across all modules.