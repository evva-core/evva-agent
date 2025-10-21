using System.Diagnostics;
using System.Runtime.InteropServices;

namespace EvvaAgent.Services
{
    public class ServiceManager
    {
        private readonly string _scriptsPath;

        public ServiceManager()
        {
            _scriptsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts");
        }

        public async Task<ServiceResult> CreateServiceAsync(ServiceConfig config)
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
                ? await ExecuteWindowsServiceCommand("create", config)
                : await ExecuteLinuxServiceCommand("create", config);
        }

        public async Task<ServiceResult> StartServiceAsync(string serviceName)
        {
            var config = new ServiceConfig { ServiceName = serviceName };
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? await ExecuteWindowsServiceCommand("start", config)
                : await ExecuteLinuxServiceCommand("start", config);
        }

        public async Task<ServiceResult> StopServiceAsync(string serviceName)
        {
            var config = new ServiceConfig { ServiceName = serviceName };
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? await ExecuteWindowsServiceCommand("stop", config)
                : await ExecuteLinuxServiceCommand("stop", config);
        }

        public async Task<ServiceResult> DeleteServiceAsync(string serviceName)
        {
            var config = new ServiceConfig { ServiceName = serviceName };
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? await ExecuteWindowsServiceCommand("delete", config)
                : await ExecuteLinuxServiceCommand("delete", config);
        }

        private async Task<ServiceResult> ExecuteWindowsServiceCommand(string action, ServiceConfig config)
        {
            var scriptPath = Path.Combine(_scriptsPath, "win", "windows-service-manager.ps1");
            var arguments = $"-Action {action} -ServiceName \"{config.ServiceName}\"";

            if (action == "create")
            {
                arguments += $" -ExecutablePath \"{config.ExecutablePath}\"";
                if (!string.IsNullOrEmpty(config.Arguments))
                    arguments += $" -Arguments \"{config.Arguments}\"";
                if (!string.IsNullOrEmpty(config.Description))
                    arguments += $" -Description \"{config.Description}\"";
            }

            return await ExecuteScript("powershell.exe", $"-File \"{scriptPath}\" {arguments}");
        }

        private async Task<ServiceResult> ExecuteLinuxServiceCommand(string action, ServiceConfig config)
        {
            var scriptPath = Path.Combine(_scriptsPath, "linux", "linux-systemd-manager.sh");
            var arguments = $"--action {action} --service-name \"{config.ServiceName}\"";

            if (action == "create")
            {
                arguments += $" --executable-path \"{config.ExecutablePath}\"";
                if (!string.IsNullOrEmpty(config.WorkingDirectory))
                    arguments += $" --working-directory \"{config.WorkingDirectory}\"";
                if (!string.IsNullOrEmpty(config.Arguments))
                    arguments += $" --arguments \"{config.Arguments}\"";
                if (!string.IsNullOrEmpty(config.Description))
                    arguments += $" --description \"{config.Description}\"";
            }

            return await ExecuteScript("/bin/bash", $"\"{scriptPath}\" {arguments}");
        }

        private async Task<ServiceResult> ExecuteScript(string fileName, string arguments)
        {
            try
            {
                using var process = new Process();
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                process.Start();
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                return new ServiceResult
                {
                    Success = process.ExitCode == 0,
                    Output = output,
                    Error = error,
                    ExitCode = process.ExitCode
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult
                {
                    Success = false,
                    Error = ex.Message,
                    ExitCode = -1
                };
            }
        }
    }

    public class ServiceConfig
    {
        public string ServiceName { get; set; } = string.Empty;
        public string ExecutablePath { get; set; } = string.Empty;
        public string WorkingDirectory { get; set; } = string.Empty;
        public string Arguments { get; set; } = string.Empty;
        public string Description { get; set; } = "EvvaCore Managed Service";
    }

    public class ServiceResult
    {
        public bool Success { get; set; }
        public string Output { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public int ExitCode { get; set; }
    }
}