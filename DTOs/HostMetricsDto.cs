using System;
using System.Collections.Generic;

namespace EvvaAgent.DTOs
{
    public class HostMetricsDto
    {
        public double CpuUsage { get; set; }
        public MemoryUsageDto MemoryUsage { get; set; }
        public List<DiskUsageDto> Disks { get; set; }
        public List<ProcessDto> TopProcesses { get; set; }
        public List<ServiceDto> Services { get; set; }
        public string Uptime { get; set; }
        public DateTime LastSeen { get; set; }
        public NetworkInfoMetricDto NetworkInfo { get; set; }
    }

    public class MemoryUsageDto
    {
        public double Used { get; set; }
        public double Total { get; set; }
        public double Percentage { get; set; }
    }

    public class NetworkInfoMetricDto
    {
        public string BytesIn { get; set; }
        public string BytesOut { get; set; }
    }

    public class DiskUsageDto
    {
        public string Name { get; set; }
        public double UsedSpaceGB { get; set; }
        public double TotalSpaceGB { get; set; }
        public double UsagePercentage { get; set; }
    }

    public class ProcessDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double CpuUsage { get; set; }
        public double MemoryUsageMB { get; set; }
        public double MemoryUsagePercentage { get; set; }
    }

    public class ServiceDto
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Status { get; set; }
    }
}
