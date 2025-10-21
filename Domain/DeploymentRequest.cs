namespace EvvaAgent.Domain;

public class DeploymentRequest
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int HostId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public List<RepositoryInfo> Repositories { get; set; } = new();
    public List<WorkflowStep> WorkflowSteps { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class RepositoryInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RepositoryUrl { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;
    public string TargetPath { get; set; } = string.Empty;
    public bool IsDockerEnabled { get; set; }
    public int? DockerConfigId { get; set; }
}

public class WorkflowStep
{
    public int Id { get; set; }
    public int ExecutionOrder { get; set; }
    public string StageName { get; set; } = string.Empty;
    public string WorkflowName { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SupportedOs { get; set; } = string.Empty;
    public string? Parameters { get; set; }
    public bool IsJsonRequired { get; set; }
    public string? JsonData { get; set; }
}