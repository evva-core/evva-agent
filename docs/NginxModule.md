# Nginx Module - Complete Documentation

## Overview

The Nginx Module is a comprehensive solution for managing Nginx web server configurations through the EvvaAgent system. It provides automated server configuration, reverse proxy setup, static site hosting, and complete Nginx lifecycle management with cross-platform support for Windows and Linux.

## Module Architecture

```
Modules/Nginx/
├── Domain/                    # 📋 Domain Models
│   └── NginxModels.cs        # Configuration models and DTOs
├── Services/                  # 🔧 Business Logic
│   └── NginxService.cs       # Core Nginx operations
├── Resources/                 # 🎯 Command Handlers
│   └── NginxResource.cs      # Command implementations
└── NginxModule.cs            # 🔌 Module Registration
```

## Key Features

- **🔄 Automatic Nginx Startup**: Starts Nginx automatically on application launch
- **🌐 Server Configuration**: Complete server block management
- **🔀 Reverse Proxy**: Easy reverse proxy configuration
- **📁 Static Sites**: Static file serving setup
- **🔧 Configuration Management**: Test, reload, and validate configurations
- **📊 Status Monitoring**: Real-time Nginx status and process information
- **🖥️ Cross-Platform**: Windows and Linux support
- **📝 UTF-8 Support**: Configuration files saved without BOM
- **🔗 Symlink Management**: Sites-available/sites-enabled pattern

## Configuration

### appsettings.json

#### Windows Configuration
```json
{
  "Nginx": {
    "ConfigPath": "C:\\nginx\\conf\\nginx.conf",
    "ExecutablePath": "C:\\nginx\\nginx.exe",
    "SitesAvailablePath": "C:\\nginx\\conf\\sites-available",
    "SitesEnabledPath": "C:\\nginx\\conf\\sites-enabled"
  }
}
```

#### Linux Configuration
```json
{
  "Nginx": {
    "ConfigPath": "/etc/nginx/nginx.conf",
    "ExecutablePath": "/usr/sbin/nginx",
    "SitesAvailablePath": "/etc/nginx/sites-available",
    "SitesEnabledPath": "/etc/nginx/sites-enabled"
  }
}
```

### Module Registration

The module is automatically registered in `Core/Extensions/ServiceCollectionExtensions.cs`:

```csharp
public static IServiceCollection AddEvvaModules(this IServiceCollection services)
{
    // Register Nginx Module
    services.AddSingleton<IEvvaModule, NginxModule>();
    services.AddScoped<NginxService>();
    services.AddScoped<NginxResource>();
    
    return services;
}
```

### Automatic Nginx Startup

Nginx is automatically started when the application launches (configured in `Program.cs`):

```csharp
// Start Nginx service
using (var scope = app.Services.CreateScope())
{
    var nginxService = scope.ServiceProvider.GetRequiredService<NginxService>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
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
```

## Available Commands

### Server Management Commands

#### evva.nginx.add.server
**Description**: Adds a complete server configuration to Nginx.

**Parameters**: JSON object with `NginxServerConfig`

**Example**:
```json
{
  "command": "evva.nginx.add.server",
  "parameters": {
    "serverName": "example.com",
    "port": 80,
    "root": "c:/wwwroot/example",
    "index": "index.html index.htm",
    "enabled": true,
    "locations": [
      {
        "path": "/",
        "tryFiles": "$uri $uri/ =404"
      }
    ],
    "customDirectives": [
      "client_max_body_size 10M"
    ]
  }
}
```

#### evva.nginx.remove.server
**Description**: Removes a server configuration from Nginx.

**Parameters**: Server name as string

**Example**:
```json
{
  "command": "evva.nginx.remove.server",
  "parameters": "example.com"
}
```

#### evva.nginx.enable.site
**Description**: Enables a site by creating symlink from sites-available to sites-enabled.

**Parameters**: Server name as string

#### evva.nginx.disable.site
**Description**: Disables a site by removing symlink from sites-enabled.

**Parameters**: Server name as string

### Quick Configuration Commands

#### evva.nginx.add.proxy
**Description**: Quickly adds a reverse proxy configuration.

**Parameters**: JSON object with `ReverseProxyConfig`

**Example**:
```json
{
  "command": "evva.nginx.add.proxy",
  "parameters": {
    "domain": "api.example.com",
    "port": 80,
    "targetUrl": "http://localhost:5000",
    "enableSSL": false,
    "headers": {
      "X-Forwarded-Proto": "$scheme",
      "X-Real-IP": "$remote_addr"
    }
  }
}
```

#### evva.nginx.add.static
**Description**: Quickly adds a static site configuration.

**Parameters**: JSON object with `StaticSiteConfig`

**Example**:
```json
{
  "command": "evva.nginx.add.static",
  "parameters": {
    "domain": "static.example.com",
    "port": 80,
    "rootPath": "c:/wwwroot/static",
    "indexFiles": "index.html index.htm",
    "enableDirectoryListing": false
  }
}
```

### Configuration Management Commands

#### evva.nginx.test.config
**Description**: Tests Nginx configuration for syntax errors.

**Parameters**: None

**Response**:
```json
{
  "success": true,
  "action": "test_config",
  "message": "Configuration test successful"
}
```

#### evva.nginx.reload
**Description**: Reloads Nginx configuration without stopping the service.

**Parameters**: None

#### evva.nginx.restart
**Description**: Completely restarts the Nginx service.

**Parameters**: None

#### evva.nginx.status
**Description**: Gets current Nginx status and process information.

**Parameters**: None

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
        "workingSet": 15728640,
        "processName": "nginx"
      }
    ]
  }
}
```

## NginxService API Reference

### Core Service Methods

#### AddServerAsync(NginxServerConfig serverConfig)
**Description**: Adds a new server configuration to Nginx with full validation and error handling.

**Features**:
- Generates complete Nginx server block
- Creates configuration file with UTF-8 encoding (no BOM)
- Automatically enables site if specified
- Tests configuration before applying
- Reloads Nginx if running
- Handles Windows/Linux path differences

**Parameters**:
- `serverConfig`: Complete server configuration object

**Returns**: `bool` - Success status

#### RemoveServerAsync(string serverName)
**Description**: Removes server configuration and cleans up files.

**Process**:
1. Disables site (removes symlink)
2. Deletes configuration file
3. Reloads Nginx if running
4. Logs all operations

#### StartNginxAsync()
**Description**: Starts Nginx service in background without blocking the application.

**Features**:
- Cross-platform support (Windows/Linux)
- Background process execution
- Status validation after startup
- Graceful error handling

**Windows Implementation**:
```csharp
private async Task<bool> StartNginxWindowsAsync()
{
    var processInfo = new ProcessStartInfo
    {
        WorkingDirectory = Path.GetDirectoryName(_nginxExecutablePath),
        FileName = _nginxExecutablePath,
        Arguments = "",
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardOutput = false,
        RedirectStandardError = false
    };

    Process.Start(processInfo);
    await Task.Delay(3000); // Wait for startup
    
    var status = await GetNginxStatusAsync();
    return status.IsRunning;
}
```

#### TestNginxConfigurationAsync()
**Description**: Tests Nginx configuration using custom success criteria.

**Features**:
- Uses `nginx -t` command
- Custom success keyword detection ("test is successful")
- Ignores warnings while detecting real errors
- Cross-platform command execution

**Implementation**:
```csharp
public async Task<bool> TestNginxConfigurationAsync()
{
    var result = await ExecuteNginxCommandAsync("-t", "test is successful");
    return result.Success;
}
```

## Generated Configuration Examples

### Basic Static Site Configuration

**Input**:
```json
{
  "serverName": "example.com",
  "port": 80,
  "root": "c:/wwwroot/example",
  "index": "index.html index.htm",
  "locations": [
    {
      "path": "/",
      "tryFiles": "$uri $uri/ =404"
    }
  ]
}
```

**Generated Nginx Configuration**:
```nginx
server {
    listen 80;
    server_name example.com;
    root c:/wwwroot/example;
    index index.html index.htm;

    error_page 404 /404.html;
    error_page 500 502 503 504 /50x.html;

    location / {
        try_files $uri $uri/ =404;
    }

    location = /50x.html {
        root c:/wwwroot/example;
    }
}
```

### Reverse Proxy Configuration

**Input**:
```json
{
  "domain": "api.example.com",
  "targetUrl": "http://localhost:5000"
}
```

**Generated Configuration**:
```nginx
server {
    listen 80;
    server_name api.example.com;

    error_page 404 /404.html;
    error_page 500 502 503 504 /50x.html;

    location / {
        proxy_pass http://localhost:5000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

## Integration Examples

### Via SignalR (Production)

**Add Reverse Proxy**:
```json
{
  "command": "evva.nginx.add.proxy",
  "parameters": {
    "domain": "api.myapp.com",
    "port": 80,
    "targetUrl": "http://localhost:3000"
  }
}
```

**Add Static Site**:
```json
{
  "command": "evva.nginx.add.static",
  "parameters": {
    "domain": "www.myapp.com",
    "port": 80,
    "rootPath": "c:/wwwroot/myapp",
    "indexFiles": "index.html"
  }
}
```

### Via REST API (Development)

**Test Configuration**:
```bash
curl -X POST http://localhost:5000/api/command \
  -H "Content-Type: application/json" \
  -d '{"command": "evva.nginx.test.config"}'
```

**Get Status**:
```bash
curl -X POST http://localhost:5000/api/command \
  -H "Content-Type: application/json" \
  -d '{"command": "evva.nginx.status"}'
```

**Reload Configuration**:
```bash
curl -X POST http://localhost:5000/api/command \
  -H "Content-Type: application/json" \
  -d '{"command": "evva.nginx.reload"}'
```

### Direct Service Usage (C#)

```csharp
// Inject NginxService
var nginxService = serviceProvider.GetRequiredService<NginxService>();

// Add server configuration
var serverConfig = new NginxServerConfig
{
    ServerName = "example.com",
    Port = 80,
    Root = "c:/wwwroot/example",
    Index = "index.html",
    Enabled = true,
    Locations = new List<NginxLocation>
    {
        new NginxLocation
        {
            Path = "/",
            TryFiles = "$uri $uri/ =404"
        }
    }
};

var success = await nginxService.AddServerAsync(serverConfig);

// Check status
var status = await nginxService.GetNginxStatusAsync();
Console.WriteLine($"Nginx running: {status.IsRunning}");
```

## Advanced Configuration Examples

### 1. Load Balancer Configuration
```json
{
  "command": "evva.nginx.add.server",
  "parameters": {
    "serverName": "app.example.com",
    "port": 80,
    "enabled": true,
    "customDirectives": [
      "upstream backend {",
      "    server 192.168.1.10:3000;",
      "    server 192.168.1.11:3000;",
      "    server 192.168.1.12:3000;",
      "}"
    ],
    "locations": [
      {
        "path": "/",
        "proxyPass": "http://backend",
        "customDirectives": [
          "proxy_set_header Host $host",
          "proxy_set_header X-Real-IP $remote_addr"
        ]
      }
    ]
  }
}
```

### 2. SSL/HTTPS Configuration
```json
{
  "command": "evva.nginx.add.server",
  "parameters": {
    "serverName": "secure.example.com",
    "port": 443,
    "root": "c:/wwwroot/secure",
    "index": "index.html",
    "enabled": true,
    "customDirectives": [
      "ssl on;",
      "ssl_certificate C:/nginx/ssl/cert.pem;",
      "ssl_certificate_key C:/nginx/ssl/key.pem;",
      "ssl_protocols TLSv1.2 TLSv1.3;",
      "ssl_ciphers ECDHE-RSA-AES256-GCM-SHA512:DHE-RSA-AES256-GCM-SHA512;"
    ],
    "locations": [
      {
        "path": "/",
        "tryFiles": "$uri $uri/ =404",
        "customDirectives": [
          "add_header Strict-Transport-Security \"max-age=31536000; includeSubDomains\" always;",
          "add_header X-Frame-Options DENY;",
          "add_header X-Content-Type-Options nosniff;"
        ]
      }
    ]
  }
}
```

### 3. SPA (Single Page Application) Configuration
```json
{
  "command": "evva.nginx.add.server",
  "parameters": {
    "serverName": "spa.example.com",
    "port": 80,
    "root": "c:/wwwroot/spa/dist",
    "index": "index.html",
    "enabled": true,
    "locations": [
      {
        "path": "/",
        "tryFiles": "$uri $uri/ /index.html",
        "customDirectives": [
          "add_header Cache-Control \"no-cache\";"
        ]
      },
      {
        "path": "/api",
        "proxyPass": "http://localhost:5000",
        "customDirectives": [
          "proxy_set_header Host $host;",
          "proxy_set_header X-Real-IP $remote_addr;"
        ]
      },
      {
        "path": "~* \\.(js|css|png|jpg|jpeg|gif|ico|svg)$",
        "customDirectives": [
          "expires 1y;",
          "add_header Cache-Control \"public, immutable\";"
        ]
      }
    ]
  }
}
```

### 4. WebSocket Proxy Configuration
```json
{
  "command": "evva.nginx.add.server",
  "parameters": {
    "serverName": "ws.example.com",
    "port": 80,
    "enabled": true,
    "locations": [
      {
        "path": "/ws",
        "proxyPass": "http://localhost:3000",
        "customDirectives": [
          "proxy_http_version 1.1;",
          "proxy_set_header Upgrade $http_upgrade;",
          "proxy_set_header Connection \"upgrade\";",
          "proxy_set_header Host $host;",
          "proxy_cache_bypass $http_upgrade;"
        ]
      }
    ]
  }
}
```

## Data Models Reference

### NginxServerConfig
**Complete server configuration model**

```csharp
public class NginxServerConfig
{
    public string ServerName { get; set; } = string.Empty;        // Domain name
    public int Port { get; set; } = 80;                          // Listen port
    public string Root { get; set; } = string.Empty;             // Document root
    public string Index { get; set; } = "index.html index.htm";  // Index files
    public bool Enabled { get; set; } = true;                    // Auto-enable site
    public List<NginxLocation> Locations { get; set; } = new();  // Location blocks
    public List<string> CustomDirectives { get; set; } = new();  // Custom directives
}
```

### NginxLocation
**Location block configuration**

```csharp
public class NginxLocation
{
    public string Path { get; set; } = "/";                      // Location path
    public string ProxyPass { get; set; } = string.Empty;        // Proxy target
    public string TryFiles { get; set; } = string.Empty;         // Try files directive
    public List<string> CustomDirectives { get; set; } = new();  // Custom directives
}
```

### ReverseProxyConfig
**Quick reverse proxy configuration**

```csharp
public class ReverseProxyConfig
{
    public string Domain { get; set; } = string.Empty;           // Domain name
    public int Port { get; set; } = 80;                          // Listen port
    public string TargetUrl { get; set; } = string.Empty;        // Backend URL
    public bool EnableSSL { get; set; } = false;                 // SSL support
    public Dictionary<string, string> Headers { get; set; } = new(); // Custom headers
}
```

### StaticSiteConfig
**Quick static site configuration**

```csharp
public class StaticSiteConfig
{
    public string Domain { get; set; } = string.Empty;           // Domain name
    public int Port { get; set; } = 80;                          // Listen port
    public string RootPath { get; set; } = string.Empty;         // Document root
    public string? IndexFiles { get; set; } = "index.html index.htm"; // Index files
    public bool EnableDirectoryListing { get; set; } = false;    // Directory listing
    public Dictionary<string, string> CustomHeaders { get; set; } = new(); // Custom headers
}
```

### NginxStatus
**Nginx status and process information**

```csharp
public class NginxStatus
{
    public bool IsRunning { get; set; }                          // Running status
    public string Status { get; set; } = string.Empty;           // Status message
    public string Error { get; set; } = string.Empty;            // Error message
    public int ProcessCount { get; set; }                        // Process count
    public List<NginxProcessInfo> Processes { get; set; } = new(); // Process details
}
```

### NginxProcessInfo
**Individual process information**

```csharp
public class NginxProcessInfo
{
    public int Id { get; set; }                                  // Process ID
    public DateTime StartTime { get; set; }                      // Start time
    public long WorkingSet { get; set; }                         // Memory usage
    public string ProcessName { get; set; } = string.Empty;      // Process name
}
```

## Error Handling & Validation

### Configuration Validation
- **Syntax Testing**: All configurations tested before applying
- **Path Validation**: Ensures paths exist and are accessible
- **Port Conflicts**: Detects port conflicts before configuration
- **UTF-8 Encoding**: Saves files without BOM for compatibility

### Error Recovery
- **Graceful Degradation**: Continues operation if Nginx is not available
- **Automatic Rollback**: Reverts changes on configuration errors
- **Detailed Logging**: Comprehensive error logging for debugging
- **Status Monitoring**: Continuous health monitoring

### Logging Examples
```
[INFO] Adding server configuration for example.com
[INFO] Nginx status: True
[INFO] Server configuration written to C:\nginx\conf\sites-available\example.com.conf
[INFO] Enabled site example.com
[INFO] Successfully added, enabled and reloaded server example.com
```

## Cross-Platform Support

### Windows Support
- **Native nginx.exe**: Direct executable support
- **Process Management**: Background process handling
- **Path Normalization**: Converts backslashes to forward slashes
- **Symlink Fallback**: Uses file copy if symlinks fail
- **Service Integration**: Windows service compatibility

### Linux Support
- **systemctl Integration**: Native systemd service management
- **Unix Commands**: Standard Unix command execution
- **Symlink Support**: Native symlink creation
- **Permission Handling**: Proper file permission management

### Automatic Detection
```csharp
if (OperatingSystem.IsWindows())
{
    return await StartNginxWindowsAsync();
}
else
{
    return await StartNginxUnixAsync();
}
```

## Performance Considerations

### Optimization Features
- **Async Operations**: All operations are asynchronous
- **Background Startup**: Nginx starts without blocking application
- **Efficient File I/O**: Optimized file operations
- **Memory Management**: Proper resource disposal
- **Connection Pooling**: Efficient process management

### Monitoring Capabilities
- **Real-time Status**: Live Nginx status monitoring
- **Process Tracking**: Individual process monitoring
- **Performance Metrics**: Memory and CPU usage tracking
- **Health Checks**: Automated health verification

## Security Features

### Configuration Security
- **Input Validation**: All parameters validated
- **Path Sanitization**: Prevents directory traversal
- **Command Injection Prevention**: Safe command execution
- **File Permission Management**: Proper file security

### Best Practices
- **Principle of Least Privilege**: Minimal required permissions
- **Secure Defaults**: Security-focused default configurations
- **Audit Logging**: All operations logged for audit
- **Error Information Filtering**: Sensitive data protection

## Troubleshooting Guide

### Common Issues

1. **Nginx Not Starting**
   - Check executable path in configuration
   - Verify file permissions
   - Check port availability
   - Review error logs

2. **Configuration Test Failures**
   - Validate JSON syntax
   - Check file paths exist
   - Verify port numbers
   - Review custom directives

3. **Symlink Creation Failures**
   - Check directory permissions
   - Verify administrator privileges (Windows)
   - Fallback to file copy automatically

4. **Cross-Platform Path Issues**
   - Use forward slashes in configuration
   - Verify path normalization
   - Check case sensitivity (Linux)

### Debug Commands
```bash
# Test Nginx configuration manually
nginx -t

# Check Nginx processes
# Windows
tasklist | findstr nginx

# Linux
ps aux | grep nginx

# Check configuration files
dir C:\nginx\conf\sites-enabled\  # Windows
ls -la /etc/nginx/sites-enabled/   # Linux
```

This comprehensive Nginx module provides enterprise-grade web server management capabilities with robust error handling, cross-platform support, and extensive configuration options.