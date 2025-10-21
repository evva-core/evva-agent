namespace EvvaAgent.Domain
{
    public class CollectMetric
    {
        public string? Date { get; set; }
        public string? CpuUsage { get; set; }
        public string? MemoryUsage { get; set; }
        public string? DiskJson { get; set; }
        public string? NetworkJson { get; set; }
        public string? ProjectsJson { get; set; }
    }
}