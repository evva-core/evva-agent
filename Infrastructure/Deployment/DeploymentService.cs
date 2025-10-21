using System;
using System.Threading.Tasks;
using EvvaAgent.Infrastructure.Execution;

namespace EvvaAgent.Infrastructure.Deployment
{
    public interface IDeploymentService
    {
        Task DeployFromGitAsync(string repositoryUrl, string branch, Action<string> log);
    }

    /// <summary>
    /// Service to handle deployment operations, specifically from Git repositories.
    /// </summary>
    public class DeploymentService : IDeploymentService
    {
        private readonly ICommandExecutorService _commandExecutor;

        public DeploymentService(ICommandExecutorService commandExecutor)
        {
            _commandExecutor = commandExecutor;
        }

        /// <summary>
        /// Clones or pulls a Git repository and performs deployment steps.
        /// </summary>
        /// <param name="repositoryUrl">The URL of the repository.</param>
        /// <param name="branch">The branch to deploy.</param>
        /// <param name="log">A callback to stream logs back to the caller.</param>
        public async Task DeployFromGitAsync(string repositoryUrl, string branch, Action<string> log)
        {
            log($"Starting deployment for {repositoryUrl} on branch {branch}");
            
            // This is a simplified example. A real implementation would be more robust.
            // It would handle folder management, build steps, etc.
            var repoName = repositoryUrl.Split('/')[^1].Replace(".git", "");
            var repoPath = $"./{repoName}";

            log($"Cloning repository into {repoPath}...");
            var cloneResult = await _commandExecutor.ExecuteCommandAsync($"git clone --branch {branch} {repositoryUrl} {repoPath}");
            log(cloneResult.Output);

            log("Deployment script would run here...");
            // e.g., await _commandExecutor.ExecuteCommandAsync($"cd {repoPath} && ./deploy.sh");

            log("Deployment finished.");
        }
    }
}
