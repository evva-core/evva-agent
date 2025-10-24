namespace EvvaAgent.Domain
{
    /// <summary>
    /// Represents a deployment operation.
    /// This class can be expanded to hold details about the repository, branch, and status.
    /// </summary>
    public class Deployment
    {
        public int Id { get; set; }
        public string RepositoryUrl { get; set; } = string.Empty;
        public string Branch { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // e.g., "InProgress", "Completed", "Failed"
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
