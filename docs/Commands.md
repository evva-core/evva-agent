# EvvaAgent - Complete Command Reference

## Overview

EvvaAgent provides a comprehensive command system for managing infrastructure services across different platforms. All commands follow the pattern: `evva.{module}.{action}.{subaction}`

## Command Categories

### 🌐 Nginx Module Commands

#### Server Management
- `evva.nginx.add.server` - Add complete server configuration
- `evva.nginx.remove.server` - Remove server configuration
- `evva.nginx.enable.site` - Enable site configuration
- `evva.nginx.disable.site` - Disable site configuration

#### Proxy & Static Sites
- `evva.nginx.add.proxy` - Add reverse proxy configuration
- `evva.nginx.add.static` - Add static site configuration

#### Service Control
- `evva.nginx.test.config` - Test configuration validity
- `evva.nginx.reload` - Reload Nginx configuration
- `evva.nginx.restart` - Restart Nginx service
- `evva.nginx.status` - Get Nginx status

### 🐧 SystemCtl Module Commands (Linux)

#### Service Lifecycle
- `evva.systemctl.create` - Create systemd service
- `evva.systemctl.start` - Start service
- `evva.systemctl.stop` - Stop service
- `evva.systemctl.restart` - Restart service
- `evva.systemctl.status` - Get service status

#### Boot Management
- `evva.systemctl.enable` - Enable service on boot
- `evva.systemctl.disable` - Disable service from boot

### 🪟 Windows Service Module Commands

#### Service Lifecycle
- `evva.winservice.create` - Create Windows service
- `evva.winservice.start` - Start service
- `evva.winservice.stop` - Stop service
- `evva.winservice.restart` - Restart service
- `evva.winservice.delete` - Delete service
- `evva.winservice.status` - Get service status

## Command Parameters

### JSON Parameters

#### Nginx Server Configuration
```json
{
  "serverName": "example.com",
  "port": 80,
  "root": "/var/www/html",
  "index": "index.html index.htm",
  "enabled": true,
  "locations": [
    {
      "path": "/",
      "tryFiles": "$uri $uri/ =404",
      "proxyPass": "",
      "customDirectives": []
    }
  ],
  "customDirectives": []
}
```

#### Reverse Proxy Configuration
```json
{
  "domain": "api.example.com",
  "port": 80,
  "targetUrl": "http://localhost:3000"
}
```

#### Static Site Configuration
```json
{
  "domain": "static.example.com",
  "port": 80,
  "rootPath": "/var/www/static",
  "indexFiles": "index.html index.htm"
}
```

#### Linux Service Configuration
```json
{
  "serviceName": "myapp",
  "displayName": "My Application",
  "executablePath": "/usr/local/bin/myapp",
  "description": "My custom Linux service",
  "startType": "Manual",
  "username": "myuser",
  "workingDirectory": "/opt/myapp"
}
```

#### Windows Service Configuration
```json
{
  "serviceName": "MyService",
  "displayName": "My Custom Service",
  "executablePath": "C:\\MyApp\\service.exe",
  "description": "My custom Windows service",
  "startType": "Manual",
  "username": null,
  "password": null
}
```

### String Parameters

Simple commands that require only a service name or identifier:
- Service name (e.g., "nginx", "apache2", "MyService")
- Server name (e.g., "example.com", "api.mysite.com")

### No Parameters

Commands that don't require any parameters:
- `evva.nginx.test.config`
- `evva.nginx.reload`
- `evva.nginx.restart`
- `evva.nginx.status`

## Command Execution Examples

### Via SignalR (from EvvaCore)

#### Create Linux Service
```json
{
  "command": "evva.systemctl.create",
  "parameters": "{\"serviceName\":\"nodeapp\",\"displayName\":\"Node.js App\",\"executablePath\":\"/usr/bin/node\",\"description\":\"My Node.js application\",\"startType\":\"Automatic\",\"username\":\"nodeuser\",\"workingDirectory\":\"/opt/nodeapp\"}"
}
```

#### Create Windows Service
```json
{
  "command": "evva.winservice.create",
  "parameters": "{\"serviceName\":\"DotNetService\",\"displayName\":\"My .NET Service\",\"executablePath\":\"C:\\\\MyApp\\\\MyApp.exe\",\"description\":\"My .NET background service\",\"startType\":\"Automatic\"}"
}
```

#### Add Nginx Reverse Proxy
```json
{
  "command": "evva.nginx.add.proxy",
  "parameters": "{\"domain\":\"api.myapp.com\",\"port\":80,\"targetUrl\":\"http://localhost:3000\"}"
}
```

#### Start Service (Linux)
```json
{
  "command": "evva.systemctl.start",
  "parameters": "nodeapp"
}
```

#### Start Service (Windows)
```json
{
  "command": "evva.winservice.start",
  "parameters": "DotNetService"
}
```

### Via REST API

#### Execute Command
```bash
curl -X POST http://localhost:5000/api/command \
  -H "Content-Type: application/json" \
  -d '{
    "command": "evva.nginx.status",
    "parameters": null
  }'
```

#### Get Available Commands
```bash
curl -X GET http://localhost:5000/api/modules
```

## Response Format

All commands return a standardized response format:

### Success Response
```json
{
  "success": true,
  "action": "create_service",
  "service": "myapp",
  "message": "Service created successfully",
  "output": "Created symlink...",
  "error": null
}
```

### Error Response
```json
{
  "success": false,
  "action": "start_service",
  "service": "myapp",
  "message": "Failed to start service",
  "output": null,
  "error": "Unit myapp.service not found"
}
```

### Status Response
```json
{
  "success": true,
  "action": "get_service_status",
  "service": "myapp",
  "data": {
    "serviceName": "myapp",
    "status": "active",
    "isActive": true,
    "isEnabled": true,
    "description": "My Application",
    "processId": 1234,
    "memoryUsage": 52428800,
    "startTime": "2024-01-15T10:30:00Z"
  }
}
```

## Error Handling

### Common Error Scenarios

1. **Invalid Command Format**
   - Missing module or action
   - Incorrect command structure

2. **Missing Parameters**
   - Required JSON configuration not provided
   - Service name not specified

3. **Platform Incompatibility**
   - Running Windows commands on Linux
   - Running Linux commands on Windows

4. **Permission Issues**
   - Insufficient privileges for service operations
   - File system access denied

5. **Service Not Found**
   - Service doesn't exist
   - Configuration file missing

### Error Recovery

- Commands are designed to be idempotent where possible
- Failed operations provide detailed error messages
- System continues operating after individual command failures
- Automatic rollback for critical operations

## Best Practices

### Command Usage

1. **Test Before Deploy**
   - Use `evva.nginx.test.config` before applying changes
   - Verify service status after operations

2. **Parameter Validation**
   - Ensure JSON is properly formatted
   - Validate file paths and URLs
   - Check service names for invalid characters

3. **Platform Awareness**
   - Use appropriate module for target platform
   - Consider path separators (/ vs \\)
   - Account for different service management systems

4. **Security Considerations**
   - Use least privilege accounts for services
   - Validate executable paths
   - Sanitize configuration inputs

### Performance Optimization

1. **Batch Operations**
   - Group related commands when possible
   - Use workflows for complex deployments

2. **Async Execution**
   - All commands execute asynchronously
   - Monitor command completion status

3. **Resource Management**
   - Commands automatically clean up resources
   - Database connections are properly managed

## Troubleshooting

### Common Issues

1. **Command Not Found**
   ```
   Check module registration in ServiceCollectionExtensions.cs
   Verify command is listed in module's GetAvailableCommands()
   ```

2. **JSON Deserialization Error**
   ```
   Validate JSON syntax
   Check property names match model properties
   Ensure enum values are strings (e.g., "Automatic")
   ```

3. **Permission Denied**
   ```
   Run agent with appropriate privileges
   Check file/directory permissions
   Verify service account permissions
   ```

4. **Service Operation Failed**
   ```
   Check service exists
   Verify service is in correct state
   Review system logs for details
   ```

### Debug Information

Enable detailed logging by setting log level to Debug in appsettings.json:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "EvvaAgent": "Debug"
    }
  }
}
```

### Log Locations

- **Windows**: `logs/evva-agent.log`
- **Linux**: `/var/log/evva-agent/evva-agent.log`
- **Console**: Real-time output during development

## Module Development

### Adding New Commands

1. **Define Command in Module**
   ```csharp
   public IEnumerable<string> GetAvailableCommands()
   {
       return new[] { "mymodule.newcommand" };
   }
   ```

2. **Implement in Resource**
   ```csharp
   ["newcommand"] = ExecuteNewCommandAsync
   ```

3. **Add Business Logic in Service**
   ```csharp
   public async Task<OperationResult> NewCommandAsync(Config config)
   {
       // Implementation
   }
   ```

4. **Register in DI Container**
   ```csharp
   services.AddSingleton<IEvvaModule, MyModule>();
   services.AddScoped<MyModuleService>();
   services.AddScoped<MyModuleResource>();
   ```

### Testing Commands

Use the built-in API endpoints for testing:

```bash
# Get all available commands
curl http://localhost:5000/api/modules

# Execute command
curl -X POST http://localhost:5000/api/command \
  -H "Content-Type: application/json" \
  -d '{"command":"evva.mymodule.newcommand","parameters":"test"}'
```