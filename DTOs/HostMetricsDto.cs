using System;
using System.Collections.Generic;

namespace EvvaAgent.DTOs
{
    public class HostMetricsDto
    {
        public double CpuUsage { get; set; }
        public MemoryUsageDto MemoryUsage { get; set; } = new();
        public List<DiskUsageDto> Disks { get; set; } = new();
        public List<ProcessDto> TopProcesses { get; set; } = new();
        public List<ServiceDto> Services { get; set; } = new();
        public string Uptime { get; set; } = string.Empty;
        public DateTime LastSeen { get; set; }
        public NetworkInfoMetricDto NetworkInfo { get; set; } = new();
    }

    public class MemoryUsageDto
    {
        public double Used { get; set; }
        public double Total { get; set; }
        public double Percentage { get; set; }
    }

    public class NetworkInfoMetricDto
    {
        public string BytesIn { get; set; } = string.Empty;
        public string BytesOut { get; set; } = string.Empty;
    }

    public class DiskUsageDto
    {
        public string Name { get; set; } = string.Empty;
        public double UsedSpaceGB { get; set; }
        public double TotalSpaceGB { get; set; }
        public double UsagePercentage { get; set; }
    }

    public class ProcessDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double CpuUsage { get; set; }
        public double MemoryUsageMB { get; set; }
        public double MemoryUsagePercentage { get; set; }
    }

    public class ServiceDto
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
