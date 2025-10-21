using EvvaAgent.DTOs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Management;
using System.Threading.Tasks;

namespace EvvaAgent.Infrastructure.Metrics
{
    public class MetricsService : IMetricsService
    {
        private readonly PerformanceCounter _cpuCounter;

        public MetricsService()
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _cpuCounter.NextValue();
        }

        public async Task<HostMetricsDto> GetHostMetricsAsync()
        {
            var memoryUsage = GetMemoryUsage();
            var topProcesses = await GetTopProcessesAsync(memoryUsage.totalMemoryBytes);

            return new HostMetricsDto
            {
                CpuUsage = Math.Round(GetCpuUsage(), 2),
                MemoryUsage = memoryUsage.dto,
                Disks = GetDiskUsage(),
                TopProcesses = topProcesses,
                Services = GetServices(),
                Uptime = GetUptime(),
                LastSeen = DateTime.UtcNow,
                NetworkInfo = GetNetworkInfo()
            };
        }

        private string GetUptime()
        {
            var os = new ManagementObjectSearcher("SELECT LastBootUpTime FROM Win32_OperatingSystem").Get().Cast<ManagementObject>().FirstOrDefault();
            if (os != null)
            {
                var lastBootUpTime = ManagementDateTimeConverter.ToDateTime(os["LastBootUpTime"].ToString());
                var uptime = DateTime.Now - lastBootUpTime;
                return uptime.TotalDays >= 1 ? $"{uptime.Days} days, {uptime.Hours} hours" : $"{uptime.Hours} hours, {uptime.Minutes} minutes";
            }
            return "N/A";
        }

        private NetworkInfoMetricDto GetNetworkInfo()
        {
            long bytesSent = 0;
            long bytesReceived = 0;

            foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up &&
                    (ni.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Ethernet ||
                     ni.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Wireless80211))
                {
                    var stats = ni.GetIPStatistics();
                    bytesSent += stats.BytesSent;
                    bytesReceived += stats.BytesReceived;
                }
            }

            return new NetworkInfoMetricDto
            {
                BytesIn = FormatBytes(bytesReceived),
                BytesOut = FormatBytes(bytesSent)
            };
        }

        private double GetCpuUsage()
        {
            return _cpuCounter.NextValue();
        }

        private (MemoryUsageDto dto, ulong totalMemoryBytes) GetMemoryUsage()
        {
            var memoryInfo = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem").Get().Cast<ManagementObject>().First();
            var totalVisibleMemorySize = (ulong)memoryInfo["TotalVisibleMemorySize"] * 1024; // Convert from KB to Bytes
            var freePhysicalMemory = (ulong)memoryInfo["FreePhysicalMemory"] * 1024; // Convert from KB to Bytes
            var usedMemory = totalVisibleMemorySize - freePhysicalMemory;

            var totalGB = Math.Round((double)totalVisibleMemorySize / (1024.0 * 1024.0 * 1024.0), 2);
            var usedGB = Math.Round((double)usedMemory / (1024.0 * 1024.0 * 1024.0), 2);
            var percentage = (double)usedMemory / totalVisibleMemorySize * 100;

            var dto = new MemoryUsageDto
            {
                Total = totalGB,
                Used = usedGB,
                Percentage = Math.Round(percentage, 2)
            };

            return (dto, totalVisibleMemorySize);
        }

        private List<DiskUsageDto> GetDiskUsage()
        {
            return DriveInfo.GetDrives()
                .Where(d => d.IsReady)
                .Select(d => new DiskUsageDto
                {
                    Name = d.Name,
                    TotalSpaceGB = Math.Round(d.TotalSize / (1024.0 * 1024 * 1024), 2),
                    UsedSpaceGB = Math.Round((d.TotalSize - d.TotalFreeSpace) / (1024.0 * 1024 * 1024), 2),
                    UsagePercentage = Math.Round((double)(d.TotalSize - d.TotalFreeSpace) / d.TotalSize * 100, 2)
                }).ToList();
        }

        private async Task<List<ProcessDto>> GetTopProcessesAsync(double totalMemoryBytes)
        {
            var processes = Process.GetProcesses()
                .OrderByDescending(p => p.WorkingSet64)
                .Take(10)
                .ToList();

            var tasks = processes.Select(async p => new ProcessDto
            {
                Id = p.Id,
                Name = p.ProcessName,
                MemoryUsageMB = Math.Round(p.WorkingSet64 / (1024.0 * 1024.0), 2),
                MemoryUsagePercentage = totalMemoryBytes > 0 ? Math.Round((double)p.WorkingSet64 / totalMemoryBytes * 100, 2) : 0,
                CpuUsage = await GetProcessCpuUsage(p)
            });

            return (await Task.WhenAll(tasks)).ToList();
        }

        private async Task<double> GetProcessCpuUsage(Process process)
        {
            try
            {
                var startTime = DateTime.UtcNow;
                var startCpuUsage = process.TotalProcessorTime;

                await Task.Delay(100);

                var endTime = DateTime.UtcNow;
                var endCpuUsage = process.TotalProcessorTime;

                var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
                var totalMsPassed = (endTime - startTime).TotalMilliseconds;
                var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);

                return Math.Round(cpuUsageTotal * 100, 2);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private List<ServiceDto> GetServices()
        {
            return ServiceController.GetServices()
                .Select(s => new ServiceDto
                {
                    Name = s.ServiceName,
                    DisplayName = s.DisplayName,
                    Status = s.Status.ToString()
                }).ToList();
        }

        private string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int i = 0;
            double dblSByte = bytes;
            while (dblSByte >= 1024 && i < suffixes.Length - 1)
            {
                dblSByte /= 1024;
                i++;
            }
            return $"{dblSByte:0.##} {suffixes[i]}";
        }
    }
}