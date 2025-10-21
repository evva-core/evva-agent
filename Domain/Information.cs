namespace EvvaAgent.Domain
{
    public class Information
    {
        public string HostName { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
        public string Os { get; set; } = string.Empty;
        public string Arch { get; set; } = string.Empty;
        public bool IsDockerHost { get; set; }
        public string Uuid { get; set; } = string.Empty;
    }
}