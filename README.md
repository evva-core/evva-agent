# EvvaAgent - Infrastructure Management System

## Overview

EvvaAgent is a modern, modular infrastructure management system built with .NET 9 and ASP.NET Core. It provides automated deployment, service management, and infrastructure monitoring through a flexible module-based architecture.

## 🚀 Key Features

- **Modular Architecture**: Extensible plugin system for different services
- **Real-time Communication**: SignalR integration with EvvaCore
- **Automated Deployments**: Git-based deployment workflows
- **Service Management**: Windows/Linux service control
- **Nginx Management**: Complete Nginx configuration automation
- **System Monitoring**: CPU, Memory, Disk metrics collection
- **Cross-Platform**: Windows and Linux support
- **Database Integration**: SQLite with Entity Framework Core

## 📋 Documentation

### Architecture & Design
- [**Architecture.md**](docs/Architecture.md) - System architecture overview
- [**ModuleSystem.md**](docs/ModuleSystem.md) - Module development guide
- [**Infrastructure.md**](docs/Infrastructure.md) - Infrastructure services
- [**Classes.md**](docs/Classes.md) - Complete API reference

### Modules & Features
- [**NginxModule.md**](docs/NginxModule.md) - Nginx automation module
- [**EvvaCommands.md**](docs/EvvaCommands.md) - Command system reference

## 🏗️ Project Structure

```
evva-agent/
├── Core/                    # Core abstractions and command system
│   ├── Abstractions/       # IEvvaModule, IEvvaResource interfaces
│   ├── Commands/           # ModularCommandService
│   └── Extensions/         # DI registration extensions
├── Modules/                # Feature modules
│   └── Nginx/             # Nginx management module
│       ├── Domain/        # Models and configurations
│       ├── Services/      # Business logic (NginxService)
│       └── Resources/     # Command implementations
├── Infrastructure/         # Cross-cutting services
│   ├── Communication/     # CoreHubService (SignalR)
│   ├── Execution/         # CommandExecutorService
│   ├── Deployment/        # DeploymentService
│   ├── Metrics/           # MetricsService
│   └── Data/              # Repository implementations
├── Domain/                 # Domain entities and repositories
├── Workers/                # Background services
├── Services/               # Legacy services (ServiceManager)
└── docs/                   # Documentation
```

## ⚡ Quick Start

### Prerequisites
- .NET 9.0 SDK
- Git (for deployments)
- Nginx (optional, for web server management)

### Installation

1. **Clone the repository**:
```bash
git clone <repository-url>
cd evva-agent
```

2. **Build the project**:
```bash
dotnet build
```

3. **Run the application**:
```bash
dotnet run
```

### Configuration

Create `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=evva-agent.db"
  },
  "Nginx": {
    "ConfigPath": "C:\\nginx\\conf\\nginx.conf",
    "ExecutablePath": "C:\\nginx\\nginx.exe",
    "SitesAvailablePath": "C:\\nginx\\conf\\sites-available",
    "SitesEnabledPath": "C:\\nginx\\conf\\sites-enabled"
  },
  "EvvaCore": {
    "HubUrl": "https://your-evva-core/hubs/agent",
    "AgentId": "unique-agent-identifier"
  }
}
```

## 🔧 Usage Examples

### Via SignalR (Production)

```json
{
  "command": "evva.nginx.add.proxy",
  "parameters": {
    "Domain": "api.example.com",
    "Port": 80,
    "TargetUrl": "http://localhost:3000"
  }
}
```

### Via REST API (Development)

```bash
# Service Management
curl -X POST http://localhost:5000/api/service/create \
  -H "Content-Type: application/json" \
  -d '{"name": "myapp", "executablePath": "/path/to/app"}'

# Start Service
curl -X POST http://localhost:5000/api/service/myapp/start

# Stop Service
curl -X POST http://localhost:5000/api/service/myapp/stop
```

## 📦 Available Commands

### Nginx Module
- `evva.nginx.add.server` - Add server configuration
- `evva.nginx.add.proxy` - Add reverse proxy
- `evva.nginx.add.static` - Add static site
- `evva.nginx.remove.server` - Remove server
- `evva.nginx.enable.site` - Enable site
- `evva.nginx.disable.site` - Disable site
- `evva.nginx.test.config` - Test configuration
- `evva.nginx.reload` - Reload Nginx
- `evva.nginx.restart` - Restart Nginx
- `evva.nginx.status` - Get Nginx status

### System Commands
- Deployment workflows
- Service management
- System metrics collection
- File operations

## 🛠️ Development

### Creating a New Module

1. **Create folder structure**:
```
Modules/MyModule/
├── Domain/              # Models and DTOs
├── Services/            # Business logic
├── Resources/           # Command implementations
└── MyModule.cs          # Module registration
```

2. **Implement IEvvaModule**:
```csharp
public class MyModule : IEvvaModule
{
    public string Name => "mymodule";
    
    public IEnumerable<string> GetAvailableCommands()
    {
        return new[] { "mymodule.action1", "mymodule.action2" };
    }
    
    public async Task<object> ExecuteCommandAsync(string command, string? parameters, IServiceProvider serviceProvider)
    {
        // Route to appropriate resource
        var parts = command.Split('.');
        var resource = parts[1]; // mymodule
        var method = string.Join('.', parts.Skip(2)); // action1
        
        // Get resource and execute
        var resourceInstance = serviceProvider.GetRequiredService<MyResource>();
        return await resourceInstance.ExecuteAsync(method, parameters, serviceProvider);
    }
}
```

3. **Register in DI**:
```csharp
// In Core/Extensions/ServiceCollectionExtensions.cs
services.AddSingleton<IEvvaModule, MyModule>();
services.AddScoped<MyResource>();
services.AddScoped<MyService>();
```

### Architecture Patterns

- **Commands**: `evva.{module}.{action}.{subaction}`
- **Namespaces**: `EvvaAgent.Modules.{Module}.{Layer}`
- **Dependency Injection**: Interface-based design
- **Logging**: Structured logging with `ILogger<T>`
- **Async/Await**: Async-first approach
- **Error Handling**: Comprehensive exception handling
- **Configuration**: `IConfiguration` for settings

### Database Integration

```csharp
// Domain entity
public class MyEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Repository interface
public interface IMyEntityRepository
{
    Task<MyEntity> GetByIdAsync(int id);
    Task<IEnumerable<MyEntity>> GetAllAsync();
    Task<MyEntity> CreateAsync(MyEntity entity);
    Task UpdateAsync(MyEntity entity);
    Task DeleteAsync(int id);
}

// Repository implementation
public class MyEntityRepository : IMyEntityRepository
{
    private readonly AgentDbContext _context;
    
    public MyEntityRepository(AgentDbContext context)
    {
        _context = context;
    }
    
    // Implementation...
}
```

## ⚙️ Configuration

### appsettings.json (Complete)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "EvvaAgent": "Debug"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=evva-agent.db"
  },
  "EvvaCore": {
    "HubUrl": "https://evva-core.example.com/hubs/agent",
    "AgentId": "agent-001",
    "ReconnectInterval": 5000,
    "MaxReconnectAttempts": 10
  },
  "Nginx": {
    "ConfigPath": "C:\\nginx\\conf\\nginx.conf",
    "ExecutablePath": "C:\\nginx\\nginx.exe",
    "SitesAvailablePath": "C:\\nginx\\conf\\sites-available",
    "SitesEnabledPath": "C:\\nginx\\conf\\sites-enabled"
  },
  "Metrics": {
    "CollectionInterval": 30000,
    "EnableSystemMetrics": true,
    "EnableServiceMetrics": true
  },
  "Deployment": {
    "WorkspaceRoot": "C:\\deployments",
    "MaxConcurrentDeployments": 3,
    "TimeoutMinutes": 30
  }
}
```

### Environment Variables

- `ASPNETCORE_ENVIRONMENT` - Environment (Development/Production)
- `EVVA_CORE_URL` - EvvaCore SignalR Hub URL
- `EVVA_AGENT_ID` - Unique agent identifier
- `NGINX_PATH` - Override Nginx executable path
- `WORKSPACE_ROOT` - Deployment workspace directory

### Windows Service Configuration

```xml
<!-- web.config for IIS -->
<configuration>
  <system.webServer>
    <handlers>
      <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
    </handlers>
    <aspNetCore processPath="dotnet" arguments=".\EvvaAgent.dll" stdoutLogEnabled="false" stdoutLogFile=".\logs\stdout" />
  </system.webServer>
</configuration>
```

## 🔍 Monitoring & Metrics

### System Metrics
- CPU usage percentage
- Memory usage (used/total)
- Disk usage (used/total)
- Network statistics
- Service status

### Application Metrics
- Command execution times
- Deployment success rates
- Module health status
- SignalR connection status

## 🐛 Troubleshooting

### Common Issues

1. **Module Registration Issues**
   ```bash
   # Check if module is registered
   # Look for "Registered module: {ModuleName}" in logs
   ```

2. **Command Execution Failures**
   ```bash
   # Verify command format
   evva.{module}.{action}.{subaction}
   
   # Check module availability
   GET /api/modules/available
   ```

3. **SignalR Connection Issues**
   ```bash
   # Check EvvaCore URL configuration
   # Verify network connectivity
   # Check authentication if required
   ```

4. **Nginx Configuration Issues**
   ```bash
   # Test Nginx configuration
   nginx -t
   
   # Check Nginx service status
   systemctl status nginx  # Linux
   # or check processes on Windows
   ```

5. **Database Issues**
   ```bash
   # Check SQLite database file permissions
   # Verify connection string
   # Run migrations: dotnet ef database update
   ```

### Logging

```bash
# View application logs
tail -f logs/evva-agent-{date}.log

# Filter by component
grep "NginxService" logs/evva-agent-*.log
grep "DeploymentService" logs/evva-agent-*.log
grep "CoreHubService" logs/evva-agent-*.log

# Check for errors
grep "ERROR" logs/evva-agent-*.log
```

### Health Checks

```bash
# Application health
GET /health

# Module status
GET /api/modules/status

# System metrics
GET /api/metrics/system
```

## 🧪 Testing

### Unit Tests
```bash
dotnet test
```

### Integration Tests
```bash
dotnet test --filter Category=Integration
```

### Manual Testing
```bash
# Test Nginx module
curl -X POST http://localhost:5000/api/test/nginx/status

# Test deployment
curl -X POST http://localhost:5000/api/test/deployment \
  -H "Content-Type: application/json" \
  -d '{"projectId": 1, "repositoryUrl": "https://github.com/user/repo.git"}'
```

## 📚 API Reference

### Service Management
- `POST /api/service/create` - Create new service
- `POST /api/service/{name}/start` - Start service
- `POST /api/service/{name}/stop` - Stop service
- `DELETE /api/service/{name}` - Delete service

### System Information
- `GET /api/system/info` - System information
- `GET /api/system/metrics` - Current metrics
- `GET /api/modules` - Available modules

## 🤝 Contributing

1. **Fork the repository**
2. **Create feature branch**: `git checkout -b feature/amazing-feature`
3. **Follow coding standards**:
   - Use async/await patterns
   - Implement proper error handling
   - Add comprehensive logging
   - Write unit tests
4. **Update documentation**
5. **Submit pull request**

### Code Style
- Follow Microsoft C# coding conventions
- Use meaningful variable and method names
- Add XML documentation for public APIs
- Implement proper exception handling

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🆘 Support

- **Documentation**: Check the `/docs` folder
- **Issues**: Create GitHub issues for bugs
- **Discussions**: Use GitHub Discussions for questions

---

**Built with ❤️ using .NET 9 and ASP.NET Core**
