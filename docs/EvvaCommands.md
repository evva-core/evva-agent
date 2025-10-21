# EvvaAgent Command System - Complete Reference

## Overview

The EvvaAgent Command System provides a unified interface for executing operations across different modules through a standardized command pattern. Commands are routed through the modular architecture and executed by appropriate resources.

## Command Architecture

### Command Flow
```
EvvaCore → SignalR → CoreHubService → ModularCommandService → Module → Resource → Service
```

### Command Pattern
```
evva.{module}.{action}.{subaction}
```

**Components**:
- `evva`: System prefix identifying internal commands
- `module`: Target module name (nginx, docker, systemctl, etc.)
- `action`: Primary action or resource
- `subaction`: Specific operation or method

## Available Modules & Commands

### Nginx Module Commands

#### Server Management
| Command | Description | Parameters | Example |
|---------|-------------|------------|---------|
| `evva.nginx.add.server` | Add complete server configuration | NginxServerConfig JSON | Complex server setup |
| `evva.nginx.remove.server` | Remove server configuration | Server name string | `"example.com"` |
| `evva.nginx.enable.site` | Enable site (create symlink) | Server name string | `"example.com"` |
| `evva.nginx.disable.site` | Disable site (remove symlink) | Server name string | `"example.com"` |

#### Quick Configuration
| Command | Description | Parameters | Example |
|---------|-------------|------------|---------|
| `evva.nginx.add.proxy` | Add reverse proxy | ReverseProxyConfig JSON | API proxy setup |
| `evva.nginx.add.static` | Add static site | StaticSiteConfig JSON | Static website |

#### Service Control
| Command | Description | Parameters | Example |
|---------|-------------|------------|---------|
| `evva.nginx.test.config` | Test configuration syntax | None | Configuration validation |
| `evva.nginx.reload` | Reload configuration | None | Apply changes |
| `evva.nginx.restart` | Restart Nginx service | None | Full restart |
| `evva.nginx.status` | Get service status | None | Health check |

### System Commands (Future Modules)

#### Docker Module (Example)
| Command | Description | Parameters |
|---------|-------------|------------|
| `evva.docker.create` | Create container | DockerConfig JSON |
| `evva.docker.start` | Start container | Container name |
| `evva.docker.stop` | Stop container | Container name |
| `evva.docker.status` | Get container status | Container name |

#### Git Module (Example)
| Command | Description | Parameters |
|---------|-------------|------------|
| `evva.git.clone` | Clone repository | Repository URL + path |
| `evva.git.pull` | Pull changes | Repository path |
| `evva.git.checkout` | Checkout branch | Branch name + path |

## Command Execution Methods

### 1. SignalR (Production)

**From EvvaCore to Agent**:
```json
{
  "command": "evva.nginx.add.proxy",
  "parameters": {
    "domain": "api.example.com",
    "port": 80,
    "targetUrl": "http://localhost:3000"
  }
}
```

**Response**:
```json
{
  "success": true,
  "action": "add_proxy",
  "data": {
    "domain": "api.example.com",
    "configFile": "/etc/nginx/sites-available/api.example.com.conf",
    "enabled": true
  }
}
```

### 2. REST API (Development)

**Execute Command**:
```bash
curl -X POST http://localhost:5000/api/command \
  -H "Content-Type: application/json" \
  -d '{
    "command": "evva.nginx.status",
    "parameters": null
  }'
```

**Response**:
```json
{
  "success": true,
  "action": "get_status",
  "data": {
    "isRunning": true,
    "processCount": 2,
    "status": "active",
    "processes": [
      {
        "id": 1234,
        "startTime": "2024-01-01T10:00:00Z",
        "workingSet": 15728640
      }
    ]
  }
}
```

### 3. Direct Service Usage (C#)

```csharp
// Via ModularCommandService
var commandService = serviceProvider.GetRequiredService<ModularCommandService>();
var result = await commandService.ExecuteCommandAsync(
    "evva.nginx.add.proxy", 
    JsonSerializer.Serialize(new ReverseProxyConfig 
    { 
        Domain = "api.example.com", 
        TargetUrl = "http://localhost:5000" 
    }),
    serviceProvider
);

// Direct service access
var nginxService = serviceProvider.GetRequiredService<NginxService>();
var success = await nginxService.AddReverseProxyAsync(proxyConfig);
```

## Parameter Formats

### Simple String Parameters
```json
{
  "command": "evva.nginx.remove.server",
  "parameters": "example.com"
}
```

### JSON Object Parameters
```json
{
  "command": "evva.nginx.add.server",
  "parameters": {
    "serverName": "example.com",
    "port": 80,
    "root": "c:/wwwroot/example",
    "index": "index.html",
    "enabled": true,
    "locations": [
      {
        "path": "/",
        "tryFiles": "$uri $uri/ =404"
      }
    ]
  }
}
```

### Complex Configuration Parameters
```json
{
  "command": "evva.nginx.add.proxy",
  "parameters": {
    "domain": "api.example.com",
    "port": 443,
    "targetUrl": "http://localhost:5000",
    "enableSSL": true,
    "headers": {
      "X-Forwarded-Proto": "$scheme",
      "X-Real-IP": "$remote_addr",
      "X-Forwarded-For": "$proxy_add_x_forwarded_for"
    }
  }
}
```

## Response Standardization

### Success Response Format
```json
{
  "success": true,
  "action": "action_name",
  "message": "Optional success message",
  "data": {
    // Action-specific data
  }
}
```

### Error Response Format
```json
{
  "success": false,
  "error": "Error description",
  "code": "ERROR_CODE",
  "details": {
    // Additional error details
  }
}
```

### Nginx Command Responses

#### Add Server Success
```json
{
  "success": true,
  "action": "add_server",
  "message": "Server configuration added successfully",
  "data": {
    "serverName": "example.com",
    "configFile": "C:\\nginx\\conf\\sites-available\\example.com.conf",
    "enabled": true,
    "nginxReloaded": true
  }
}
```

#### Status Response
```json
{
  "success": true,
  "action": "get_status",
  "data": {
    "isRunning": true,
    "processCount": 2,
    "status": "active",
    "processes": [
      {
        "id": 1234,
        "startTime": "2024-01-01T10:00:00Z",
        "workingSet": 15728640,
        "processName": "nginx"
      }
    ]
  }
}
```

#### Configuration Test Response
```json
{
  "success": true,
  "action": "test_config",
  "message": "Configuration test successful",
  "data": {
    "output": "nginx: configuration file test is successful",
    "warnings": [
      "nginx: [warn] conflicting server name \"localhost\" on 0.0.0.0:80, ignored"
    ]
  }
}
```

## Deployment Integration

### Workflow Commands
Commands can be integrated into deployment workflows:

```json
{
  "workflowSteps": [
    {
      "workflowName": "Setup Infrastructure",
      "stageName": "Configure Reverse Proxy",
      "command": "evva.nginx.add.proxy",
      "parameters": "{\"domain\":\"api.myapp.com\",\"targetUrl\":\"http://localhost:3000\"}",
      "executionOrder": 1,
      "description": "Setup API reverse proxy"
    },
    {
      "workflowName": "Setup Infrastructure", 
      "stageName": "Configure Static Site",
      "command": "evva.nginx.add.static",
      "parameters": "{\"domain\":\"www.myapp.com\",\"rootPath\":\"c:/wwwroot/myapp\"}",
      "executionOrder": 2,
      "description": "Setup static website"
    },
    {
      "workflowName": "Finalize Setup",
      "stageName": "Test Configuration",
      "command": "evva.nginx.test.config",
      "executionOrder": 3,
      "description": "Validate Nginx configuration"
    },
    {
      "workflowName": "Finalize Setup",
      "stageName": "Reload Nginx",
      "command": "evva.nginx.reload",
      "executionOrder": 4,
      "description": "Apply configuration changes"
    }
  ]
}
```

## Command Discovery

### Available Commands API
```bash
# Get all available commands
curl -X GET http://localhost:5000/api/modules

# Response
{
  "modules": [
    {
      "name": "nginx",
      "commands": [
        "evva.nginx.add.server",
        "evva.nginx.add.proxy",
        "evva.nginx.add.static",
        "evva.nginx.remove.server",
        "evva.nginx.enable.site",
        "evva.nginx.disable.site",
        "evva.nginx.test.config",
        "evva.nginx.reload",
        "evva.nginx.restart",
        "evva.nginx.status"
      ]
    }
  ]
}
```

### Command Validation
```csharp
public class CommandValidator
{
    private readonly ModularCommandService _commandService;

    public bool IsValidCommand(string command)
    {
        return _commandService.IsCommandAvailable(command);
    }

    public IEnumerable<string> GetAvailableCommands()
    {
        return _commandService.GetAvailableCommands();
    }

    public IEvvaModule? GetModuleForCommand(string command)
    {
        var parts = command.Split('.');
        if (parts.Length >= 2 && parts[0] == "evva")
        {
            return _commandService.GetModule(parts[1]);
        }
        return null;
    }
}
```

## Error Handling

### Command Validation Errors
```json
{
  "success": false,
  "error": "Invalid command format",
  "code": "INVALID_COMMAND_FORMAT",
  "details": {
    "command": "invalid.command",
    "expectedFormat": "evva.{module}.{action}.{subaction}"
  }
}
```

### Module Not Found Errors
```json
{
  "success": false,
  "error": "Module 'unknown' not found",
  "code": "MODULE_NOT_FOUND",
  "details": {
    "availableModules": ["nginx"]
  }
}
```

### Parameter Validation Errors
```json
{
  "success": false,
  "error": "Invalid parameters for command",
  "code": "INVALID_PARAMETERS",
  "details": {
    "command": "evva.nginx.add.server",
    "error": "ServerName is required",
    "receivedParameters": "{\"port\": 80}"
  }
}
```

### Execution Errors
```json
{
  "success": false,
  "error": "Nginx configuration test failed",
  "code": "NGINX_CONFIG_ERROR",
  "details": {
    "output": "nginx: [emerg] unexpected \"}\" in /etc/nginx/sites-available/example.com.conf:15",
    "configFile": "/etc/nginx/sites-available/example.com.conf",
    "line": 15
  }
}
```

## Creating Custom Commands

### 1. Implement IEvvaResource
```csharp
public class CustomResource : IEvvaResource
{
    private readonly Dictionary<string, Func<string?, IServiceProvider, Task<object>>> _methods;

    public CustomResource()
    {
        _methods = new Dictionary<string, Func<string?, IServiceProvider, Task<object>>>
        {
            ["action1"] = ExecuteAction1Async,
            ["action2"] = ExecuteAction2Async
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

    private async Task<object> ExecuteAction1Async(string? parameters, IServiceProvider serviceProvider)
    {
        // Implementation
        return new { success = true, action = "action1", data = "result" };
    }
}
```

### 2. Implement IEvvaModule
```csharp
public class CustomModule : IEvvaModule
{
    public string Name => "custom";

    public IEnumerable<string> GetAvailableCommands()
    {
        return new[] { "custom.action1", "custom.action2" };
    }

    public async Task<object> ExecuteCommandAsync(string command, string? parameters, IServiceProvider serviceProvider)
    {
        var parts = command.Split('.');
        if (parts.Length < 3 || parts[0] != "evva" || parts[1] != "custom")
        {
            return new { success = false, error = "Invalid command format" };
        }

        var method = parts[2];
        
        using var scope = serviceProvider.CreateScope();
        var resource = scope.ServiceProvider.GetRequiredService<CustomResource>();
        
        return await resource.ExecuteAsync(method, parameters, serviceProvider);
    }
}
```

### 3. Register Module
```csharp
// In ServiceCollectionExtensions.cs
services.AddSingleton<IEvvaModule, CustomModule>();
services.AddScoped<CustomResource>();
services.AddScoped<CustomService>();
```

## Performance Considerations

### Async Execution
- All commands execute asynchronously
- Non-blocking operations
- Proper resource disposal
- Timeout handling

### Resource Management
- Scoped service resolution
- Automatic cleanup
- Memory optimization
- Connection pooling

### Error Recovery
- Graceful error handling
- Automatic retry mechanisms
- Rollback capabilities
- State consistency

## Security Considerations

### Input Validation
- Parameter sanitization
- Command format validation
- SQL injection prevention
- Path traversal protection

### Authorization
- Command-level permissions
- Module access control
- Audit logging
- Secure communication

### Data Protection
- Sensitive parameter filtering
- Secure configuration storage
- Encrypted communication
- Access logging

## Monitoring & Observability

### Command Metrics
- Execution time tracking
- Success/failure rates
- Resource utilization
- Performance monitoring

### Logging
- Structured command logging
- Correlation ID tracking
- Error context capture
- Audit trail maintenance

### Health Checks
- Module health monitoring
- Command availability checks
- Resource status validation
- System health reporting

## Best Practices

### Command Design
1. **Consistent Naming**: Follow the `evva.{module}.{action}.{subaction}` pattern
2. **Clear Parameters**: Use descriptive parameter names and structures
3. **Standardized Responses**: Follow the success/error response format
4. **Comprehensive Validation**: Validate all inputs thoroughly

### Error Handling
1. **Graceful Degradation**: Handle errors without system failure
2. **Detailed Logging**: Provide comprehensive error information
3. **User-Friendly Messages**: Return clear error descriptions
4. **Recovery Mechanisms**: Implement automatic recovery where possible

### Performance
1. **Async Operations**: Use async/await patterns consistently
2. **Resource Cleanup**: Properly dispose of resources
3. **Caching**: Cache frequently accessed data
4. **Optimization**: Monitor and optimize slow operations

### Security
1. **Input Sanitization**: Validate and sanitize all inputs
2. **Least Privilege**: Grant minimal required permissions
3. **Audit Logging**: Log all security-relevant operations
4. **Secure Defaults**: Use secure configuration defaults

This comprehensive command system provides a robust, extensible foundation for managing infrastructure operations through a unified interface while maintaining security, performance, and reliability standards.