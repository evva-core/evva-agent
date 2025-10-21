using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using EvvaAgent.Modules.Nginx.Domain;

namespace EvvaAgent.Modules.Nginx.Services
{
    public class NginxService
    {
        private readonly ILogger<NginxService> _logger;
        private readonly string _nginxConfigPath;
        private readonly string _nginxExecutablePath;
        private readonly string _sitesAvailablePath;
        private readonly string _sitesEnabledPath;

        public NginxService(ILogger<NginxService> logger, IConfiguration configuration)
        {
            _logger = logger;
            
            // Get paths from configuration or use defaults
            _nginxConfigPath = configuration["Nginx:ConfigPath"] ?? GetDefaultNginxConfigPath();
            _nginxExecutablePath = configuration["Nginx:ExecutablePath"] ?? GetDefaultNginxExecutablePath();
            _sitesAvailablePath = configuration["Nginx:SitesAvailablePath"] ?? Path.Combine(Path.GetDirectoryName(_nginxConfigPath)!, "sites-available");
            _sitesEnabledPath = configuration["Nginx:SitesEnabledPath"] ?? Path.Combine(Path.GetDirectoryName(_nginxConfigPath)!, "sites-enabled");
            
            // Ensure directories exist
            EnsureDirectoriesExist();
        }

        /// <summary>
        /// Add a new server configuration to Nginx
        /// </summary>
        public async Task<bool> AddServerAsync(NginxServerConfig serverConfig)
        {
            try
            {
                _logger.LogInformation("Adding server configuration for {ServerName}", serverConfig.ServerName);

                // Check if Nginx is running
                var status = await GetNginxStatusAsync();
                var isNginxRunning = status.IsRunning;
                
                _logger.LogInformation("Nginx status: {IsRunning}", isNginxRunning);

                // Generate server configuration
                var configContent = GenerateServerConfig(serverConfig);
                
                // Write to sites-available
                var configFileName = $"{serverConfig.ServerName}.conf";
                var configFilePath = Path.Combine(_sitesAvailablePath, configFileName);
                
                await File.WriteAllTextAsync(configFilePath, configContent, new UTF8Encoding(false));
                _logger.LogInformation("Server configuration written to {ConfigPath}", configFilePath);

                // Enable site by creating symlink
                if (serverConfig.Enabled)
                {
                    await EnableSiteAsync(serverConfig.ServerName);
                }

                // Only test and reload if Nginx is running
                if (isNginxRunning)
                {
                    // Test configuration
                    if (!await TestNginxConfigurationAsync())
                    {
                        _logger.LogError("Nginx configuration test failed for {ServerName}", serverConfig.ServerName);
                        return false;
                    }

                    // Reload Nginx
                    if (!await ReloadNginxAsync())
                    {
                        _logger.LogError("Failed to reload Nginx after adding {ServerName}", serverConfig.ServerName);
                        return false;
                    }
                    
                    _logger.LogInformation("Successfully added, enabled and reloaded server {ServerName}", serverConfig.ServerName);
                }
                else
                {
                    _logger.LogInformation("Successfully added and enabled server {ServerName} (Nginx not running - skipped reload)", serverConfig.ServerName);
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding server configuration for {ServerName}", serverConfig.ServerName);
                return false;
            }
        }

        /// <summary>
        /// Remove a server configuration from Nginx
        /// </summary>
        public async Task<bool> RemoveServerAsync(string serverName)
        {
            try
            {
                _logger.LogInformation("Removing server configuration for {ServerName}", serverName);

                // Check if Nginx is running
                var status = await GetNginxStatusAsync();
                var isNginxRunning = status.IsRunning;

                // Disable site
                await DisableSiteAsync(serverName);

                // Remove configuration file
                var configFileName = $"{serverName}.conf";
                var configFilePath = Path.Combine(_sitesAvailablePath, configFileName);
                
                if (File.Exists(configFilePath))
                {
                    File.Delete(configFilePath);
                    _logger.LogInformation("Removed configuration file {ConfigPath}", configFilePath);
                }

                // Only reload if Nginx is running
                if (isNginxRunning)
                {
                    if (!await ReloadNginxAsync())
                    {
                        _logger.LogError("Failed to reload Nginx after removing {ServerName}", serverName);
                        return false;
                    }
                    _logger.LogInformation("Successfully removed and reloaded server {ServerName}", serverName);
                }
                else
                {
                    _logger.LogInformation("Successfully removed server {ServerName} (Nginx not running - skipped reload)", serverName);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing server configuration for {ServerName}", serverName);
                return false;
            }
        }

        /// <summary>
        /// Enable a site by creating symlink
        /// </summary>
        public async Task<bool> EnableSiteAsync(string serverName)
        {
            try
            {
                var configFileName = $"{serverName}.conf";
                var sourcePath = Path.Combine(_sitesAvailablePath, configFileName);
                var targetPath = Path.Combine(_sitesEnabledPath, configFileName);

                if (!File.Exists(sourcePath))
                {
                    _logger.LogError("Configuration file not found: {SourcePath}", sourcePath);
                    return false;
                }

                // Remove existing symlink if exists
                if (File.Exists(targetPath))
                {
                    File.Delete(targetPath);
                }

                // Create symlink (Windows) or copy file (fallback)
                if (OperatingSystem.IsWindows())
                {
                    await CreateSymlinkWindowsAsync(sourcePath, targetPath);
                }
                else
                {
                    await CreateSymlinkUnixAsync(sourcePath, targetPath);
                }

                _logger.LogInformation("Enabled site {ServerName}", serverName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enabling site {ServerName}", serverName);
                return false;
            }
        }

        /// <summary>
        /// Disable a site by removing symlink
        /// </summary>
        public async Task<bool> DisableSiteAsync(string serverName)
        {
            try
            {
                var configFileName = $"{serverName}.conf";
                var targetPath = Path.Combine(_sitesEnabledPath, configFileName);

                if (File.Exists(targetPath))
                {
                    File.Delete(targetPath);
                    _logger.LogInformation("Disabled site {ServerName}", serverName);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disabling site {ServerName}", serverName);
                return false;
            }
        }

        /// <summary>
        /// Test Nginx configuration
        /// </summary>
        public async Task<bool> TestNginxConfigurationAsync()
        {
            try
            {
                var result = await ExecuteNginxCommandAsync("-t", "test is successful");
                return result.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing Nginx configuration");
                return false;
            }
        }

        /// <summary>
        /// Reload Nginx configuration
        /// </summary>
        public async Task<bool> ReloadNginxAsync()
        {
            try
            {
                var result = await ExecuteNginxCommandAsync("-s reload");
                if (result.Success)
                {
                    _logger.LogInformation("Nginx reloaded successfully");
                    return true;
                }
                else
                {
                    _logger.LogError("Failed to reload Nginx: {Error}", result.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reloading Nginx");
                return false;
            }
        }

        /// <summary>
        /// Start Nginx service in background
        /// </summary>
        public async Task<bool> StartNginxAsync()
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    return await StartNginxWindowsAsync();
                }
                else
                {
                    return await StartNginxUnixAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting Nginx");
                return false;
            }
        }

        /// <summary>
        /// Restart Nginx service
        /// </summary>
        public async Task<bool> RestartNginxAsync()
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    return await RestartNginxWindowsAsync();
                }
                else
                {
                    return await RestartNginxUnixAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restarting Nginx");
                return false;
            }
        }

        /// <summary>
        /// Get Nginx status
        /// </summary>
        public async Task<NginxStatus> GetNginxStatusAsync()
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    return await GetNginxStatusWindowsAsync();
                }
                else
                {
                    return await GetNginxStatusUnixAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Nginx status");
                return new NginxStatus { IsRunning = false, Error = ex.Message };
            }
        }

        private string GenerateServerConfig(NginxServerConfig config)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("server {");
            sb.AppendLine($"    listen {config.Port};");
            
            if (!string.IsNullOrEmpty(config.ServerName))
            {
                sb.AppendLine($"    server_name {config.ServerName};");
            }

            if (!string.IsNullOrEmpty(config.Root))
            {
                sb.AppendLine($"    root {config.Root};");
            }

            if (!string.IsNullOrEmpty(config.Index))
            {
                sb.AppendLine($"    index {config.Index};");
            }

            // Add default error pages
            sb.AppendLine();
            sb.AppendLine("    error_page 404 /404.html;");
            sb.AppendLine("    error_page 500 502 503 504 /50x.html;");

            // Add locations
            foreach (var location in config.Locations)
            {
                sb.AppendLine();
                sb.AppendLine($"    location {location.Path} {{");
                
                if (!string.IsNullOrEmpty(location.ProxyPass))
                {
                    sb.AppendLine($"        proxy_pass {location.ProxyPass};");
                    sb.AppendLine("        proxy_set_header Host $host;");
                    sb.AppendLine("        proxy_set_header X-Real-IP $remote_addr;");
                    sb.AppendLine("        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;");
                    sb.AppendLine("        proxy_set_header X-Forwarded-Proto $scheme;");
                }

                if (!string.IsNullOrEmpty(location.TryFiles))
                {
                    sb.AppendLine($"        try_files {location.TryFiles};");
                }

                foreach (var directive in location.CustomDirectives)
                {
                    sb.AppendLine($"        {directive};");
                }

                sb.AppendLine("    }");
            }

            // Add error page locations if root is defined
            if (!string.IsNullOrEmpty(config.Root))
            {
                sb.AppendLine();
                sb.AppendLine("    location = /50x.html {");
                sb.AppendLine($"        root {config.Root};");
                sb.AppendLine("    }");
            }

            // Add custom directives
            foreach (var directive in config.CustomDirectives)
            {
                sb.AppendLine($"    {directive};");
            }

            sb.AppendLine("}");

            return sb.ToString();
        }

        private async Task<(bool Success, string Output, string Error)> ExecuteNginxCommandAsync(string arguments, string successKeyword = null)
        {
            var processInfo = new ProcessStartInfo
            {
                WorkingDirectory = Path.GetDirectoryName(_nginxExecutablePath),
                FileName = _nginxExecutablePath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
            {
                return (false, "", "Failed to start Nginx process");
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            
            await process.WaitForExitAsync();

            // If successKeyword is provided, check for it in output or error
            bool success = process.ExitCode == 0;
            if (!string.IsNullOrEmpty(successKeyword))
            {
                success = (output + error).Contains(successKeyword, StringComparison.OrdinalIgnoreCase);
            }

            return (success, output, error);
        }

        private async Task CreateSymlinkWindowsAsync(string sourcePath, string targetPath)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    WorkingDirectory = _nginxConfigPath.Replace("nginx.conf", "sites-enabled"),
                    FileName = "cmd.exe",
                    Arguments = $"/c mklink \"{targetPath}\" \"{sourcePath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    if (process.ExitCode != 0)
                    {
                        _logger.LogWarning("Symlink creation failed, falling back to file copy");
                        File.Copy(sourcePath, targetPath, true);
                    }
                }
                else
                {
                    _logger.LogWarning("Failed to start mklink process, falling back to file copy");
                    File.Copy(sourcePath, targetPath, true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error creating symlink, falling back to file copy");
                File.Copy(sourcePath, targetPath, true);
            }
        }

        private async Task CreateSymlinkUnixAsync(string sourcePath, string targetPath)
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "/bin/ln",
                Arguments = $"-sf \"{sourcePath}\" \"{targetPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process != null)
            {
                await process.WaitForExitAsync();
            }
        }

        private async Task<bool> StartNginxWindowsAsync()
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    WorkingDirectory = _nginxExecutablePath.Replace("nginx.exe", ""),
                    FileName = _nginxExecutablePath,
                    Arguments = "",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false
                };

                Process.Start(processInfo);
                _logger.LogInformation("Nginx process started in background");
                
                // Wait a moment and validate
                await Task.Delay(3000);
                var status = await GetNginxStatusAsync();
                return status.IsRunning;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting Nginx on Windows");
                return false;
            }
        }

        private async Task<bool> StartNginxUnixAsync()
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "/bin/systemctl",
                Arguments = "start nginx",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null) return false;

            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }

        private async Task<bool> RestartNginxWindowsAsync()
        {
            // Stop Nginx
            await ExecuteNginxCommandAsync("-s quit");
            await Task.Delay(2000); // Wait for graceful shutdown

            // Start Nginx
            return await StartNginxWindowsAsync();
        }

        private async Task<bool> RestartNginxUnixAsync()
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "/bin/systemctl",
                Arguments = "restart nginx",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null) return false;

            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }

        private async Task<NginxStatus> GetNginxStatusWindowsAsync()
        {
            var processes = Process.GetProcessesByName("nginx");
            return new NginxStatus
            {
                IsRunning = processes.Length > 0,
                ProcessCount = processes.Length,
                Processes = processes.Select(p => new NginxProcessInfo
                {
                    Id = p.Id,
                    StartTime = p.StartTime,
                    WorkingSet = p.WorkingSet64
                }).ToList()
            };
        }

        private async Task<NginxStatus> GetNginxStatusUnixAsync()
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "/bin/systemctl",
                Arguments = "is-active nginx",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
            {
                return new NginxStatus { IsRunning = false, Error = "Failed to check status" };
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            return new NginxStatus
            {
                IsRunning = output.Trim() == "active",
                Status = output.Trim()
            };
        }

        private string GetDefaultNginxConfigPath()
        {
            if (OperatingSystem.IsWindows())
            {
                return @"C:\nginx\conf\nginx.conf";
            }
            else
            {
                return "/etc/nginx/nginx.conf";
            }
        }

        private string GetDefaultNginxExecutablePath()
        {
            if (OperatingSystem.IsWindows())
            {
                return @"C:\nginx\nginx.exe";
            }
            else
            {
                return "/usr/sbin/nginx";
            }
        }

        private void EnsureDirectoriesExist()
        {
            try
            {
                if (!Directory.Exists(_sitesAvailablePath))
                {
                    Directory.CreateDirectory(_sitesAvailablePath);
                    _logger.LogInformation("Created sites-available directory: {Path}", _sitesAvailablePath);
                }

                if (!Directory.Exists(_sitesEnabledPath))
                {
                    Directory.CreateDirectory(_sitesEnabledPath);
                    _logger.LogInformation("Created sites-enabled directory: {Path}", _sitesEnabledPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Nginx directories");
            }
        }
    }


}