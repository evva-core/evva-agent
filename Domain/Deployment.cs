namespace EvvaAgent.Domain
{
    /// <summary>
    /// Represents a deployment operation.
    /// This class can be expanded to hold details about the repository, branch, and status.
    /// </summary>
    public class Deployment
    {
        public string RepositoryUrl { get; set; }
        public string Branch { get; set; }
        public string Status { get; set; } // e.g., "InProgress", "Completed", "Failed"
    }
}
