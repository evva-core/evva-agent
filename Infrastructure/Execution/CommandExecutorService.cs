using System.Diagnostics;

namespace EvvaAgent.Infrastructure.Execution
{
    public interface ICommandExecutorService
    {
        Task<CommandResult> ExecuteCommandAsync(string command);
        Task<CommandResult> ExecuteCommandInDirectoryAsync(string command, string workingDirectory);
    }

    public class CommandResult
    {
        public string Output { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public int ExitCode { get; set; }
    }

    public class CommandExecutorService : ICommandExecutorService
    {
        public Task<CommandResult> ExecuteCommandAsync(string command)
        {
            return ExecuteCommandInDirectoryAsync(command, Environment.CurrentDirectory);
        }

        public async Task<CommandResult> ExecuteCommandInDirectoryAsync(string command, string workingDirectory)
        {
            var isWindows = OperatingSystem.IsWindows();
            Process? process = null;
            
            try
            {
                process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = isWindows ? "cmd.exe" : "/bin/bash",
                        Arguments = isWindows ? $"/c {command}" : $"-c \"{command}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory
                    }
                };

                process.Start();
                
                // Set timeout to prevent hanging processes
                var timeoutTask = Task.Delay(TimeSpan.FromMinutes(10));
                var processTask = process.WaitForExitAsync();
                
                var completedTask = await Task.WhenAny(processTask, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    // Timeout occurred
                    try
                    {
                        process.Kill(true); // Kill entire process tree
                    }
                    catch { /* Ignore kill errors */ }
                    
                    return new CommandResult
                    {
                        Output = "",
                        Error = "Command timed out after 10 minutes",
                        ExitCode = -1
                    };
                }
                
                // Process completed normally
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                
                return new CommandResult
                {
                    Output = output,
                    Error = error,
                    ExitCode = process.ExitCode
                };
            }
            catch (Exception ex)
            {
                return new CommandResult
                {
                    Output = "",
                    Error = $"Command execution failed: {ex.Message}",
                    ExitCode = -1
                };
            }
            finally
            {
                process?.Dispose();
            }
        }
    }
}