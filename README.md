# EvvaAgent - Documentation

## Overview

EvvaAgent is a modular system for infrastructure management and deployments, built with a module and command-based architecture.

## Documentation

### 📋 Architecture

- [**Architecture.md**](docs/Architecture.md) - Modular architecture overview
- [**ModuleSystem.md**](docs/ModuleSystem.md) - Module and command system
- [**Infrastructure.md**](docs/Infrastructure.md) - Infrastructure services
- [**Classes.md**](docs/Classes.md) - Complete class reference

### 🔧 Specific Modules

- [**NginxModule.md**](docs/NginxModule.md) - Nginx Module (legacy)
- [**EvvaCommands.md**](docs/EvvaCommands.md) - Evva Commands (legacy)

## Quick Start

### 1. Project Structure

```
evva-agent/
├── Core/           # Central command system
├── Modules/        # Specific modules (Nginx, Docker, etc.)
├── Infrastructure/ # Infrastructure services
├── Domain/         # Domain entities
└── docs/          # This documentation
```

### 2. Executing Commands

#### Via SignalR

```json
{
  "command": "evva.nginx.add.proxy",
  "parameters": {
    "Domain": "api.exemplo.com",
    "Port": 80,
    "TargetUrl": "http://localhost:3000"
  }
}
```
### 3. Available Commands

#### Nginx

- `evva.nginx.add.server` - Add server
- `evva.nginx.add.proxy` - Add reverse proxy
- `evva.nginx.add.static` - Add static site
- `evva.nginx.reload` - Reload configuration
- `evva.nginx.status` - Nginx status

## Development

### Creating a New Module

1. **Create folder structure**:

```
Modules/MeuModulo/
├── Domain/
├── Services/
└── Resources/
```

2. **Implement IEvvaModule**:

```csharp
public class MeuModulo : IEvvaModule
{
    public string Name => "meumodulo";
    // ... implementation
}
```

3. **Register in DI**:

```csharp
services.AddSingleton<IEvvaModule, MeuModulo>();
```

### Code Patterns

- **Commands**: `evva.{module}.{action}.{subaction}`
- **Namespaces**: `EvvaAgent.Modules.{Module}.{Layer}`
- **Dependency Injection**: Always use interfaces
- **Logging**: `ILogger<T>` in all services
- **Async/Await**: Always use async operations

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=evva-agent.db"
  },
  "Nginx": {
    "ConfigPath": "/etc/nginx/nginx.conf",
    "ExecutablePath": "/usr/sbin/nginx"
  }
}
```

### Environment Variables

- `ASPNETCORE_ENVIRONMENT` - Environment (Development/Production)
- `EVVA_CORE_URL` - EvvaCore URL
- `EVVA_AGENT_ID` - Unique agent ID

## Troubleshooting

### Common Issues

1. **Module not found**

   - Check if registered in DI
   - Check correct namespace
2. **Command not executed**

   - Check format: `evva.module.command`
   - Check if module is active
3. **Core connection error**

   - Check CoreHub URL
   - Check network connectivity

### Logs

```bash
# View logs in real time
tail -f logs/evva-agent.log

# Filter by module
grep "nginx" logs/evva-agent.log
```

## Contributing

1. Fork the repository
2. Create feature branch
3. Implement following patterns
4. Add tests
5. Update documentation
6. Create Pull Request
