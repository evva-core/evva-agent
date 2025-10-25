using EvvaAgent.DTOs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EvvaAgent.Infrastructure.Metrics
{
    public class LinuxMetricsService : IlinuxMetricsService
    {
        public async Task<HostMetricsDto> GetHostMetricsAsync()
        {
            var memoryUsage = await GetMemoryUsageAsync();
            var topProcesses = await GetTopProcessesAsync(memoryUsage.totalMemoryBytes);

            var cpuUsage = await GetCpuUsageAsync();
            
            return new HostMetricsDto
            {
                CpuUsage = double.IsFinite(cpuUsage) ? Math.Round(cpuUsage, 2) : 0,
                MemoryUsage = memoryUsage.dto,
                Disks = GetDiskUsage(),
                TopProcesses = topProcesses,
                Services = GetServices(),
                Uptime = GetUptime(),
                LastSeen = DateTime.UtcNow,
                NetworkInfo = GetNetworkInfo()
            };
        }

        private async Task<double> GetCpuUsageAsync()
        {
            try
            {
                var cpuInfo1 = await ReadCpuInfoAsync();
                await Task.Delay(1000);
                var cpuInfo2 = await ReadCpuInfoAsync();

                var idle1 = cpuInfo1.idle + cpuInfo1.iowait;
                var idle2 = cpuInfo2.idle + cpuInfo2.iowait;

                var nonIdle1 = cpuInfo1.user + cpuInfo1.nice + cpuInfo1.system + cpuInfo1.irq + cpuInfo1.softirq + cpuInfo1.steal;
                var nonIdle2 = cpuInfo2.user + cpuInfo2.nice + cpuInfo2.system + cpuInfo2.irq + cpuInfo2.softirq + cpuInfo2.steal;

                var total1 = idle1 + nonIdle1;
                var total2 = idle2 + nonIdle2;

                var totald = total2 - total1;
                var idled = idle2 - idle1;

                return totald != 0 ? (totald - idled) * 100.0 / totald : 0;
            }
            catch
            {
                return 0;
            }
        }

        private async Task<(long user, long nice, long system, long idle, long iowait, long irq, long softirq, long steal)> ReadCpuInfoAsync()
        {
            var lines = await File.ReadAllLinesAsync("/proc/stat");
            var cpuLine = lines[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            return (
                user: long.Parse(cpuLine[1]),
                nice: long.Parse(cpuLine[2]),
                system: long.Parse(cpuLine[3]),
                idle: long.Parse(cpuLine[4]),
                iowait: long.Parse(cpuLine[5]),
                irq: long.Parse(cpuLine[6]),
                softirq: long.Parse(cpuLine[7]),
                steal: cpuLine.Length > 8 ? long.Parse(cpuLine[8]) : 0
            );
        }

        private async Task<(MemoryUsageDto dto, ulong totalMemoryBytes)> GetMemoryUsageAsync()
        {
            try
            {
                var lines = await File.ReadAllLinesAsync("/proc/meminfo");
                var memInfo = lines.Take(10).ToDictionary(
                    line => line.Split(':')[0].Trim(),
                    line => long.Parse(line.Split(':')[1].Trim().Split(' ')[0]) * 1024
                );

                var totalMemory = (ulong)memInfo["MemTotal"];
                var freeMemory = (ulong)memInfo["MemFree"];
                var buffers = memInfo.ContainsKey("Buffers") ? (ulong)memInfo["Buffers"] : 0;
                var cached = memInfo.ContainsKey("Cached") ? (ulong)memInfo["Cached"] : 0;
                
                var usedMemory = totalMemory - freeMemory - buffers - cached;

                var totalGB = Math.Round((double)totalMemory / (1024.0 * 1024.0 * 1024.0), 2);
                var usedGB = Math.Round((double)usedMemory / (1024.0 * 1024.0 * 1024.0), 2);
                var percentage = (double)usedMemory / totalMemory * 100;

                var dto = new MemoryUsageDto
                {
                    Total = double.IsFinite(totalGB) ? totalGB : 0,
                    Used = double.IsFinite(usedGB) ? usedGB : 0,
                    Percentage = double.IsFinite(percentage) ? Math.Round(percentage, 2) : 0
                };

                return (dto, totalMemory);
            }
            catch
            {
                return (new MemoryUsageDto(), 0);
            }
        }

        private List<DiskUsageDto> GetDiskUsage()
        {
            return DriveInfo.GetDrives()
                .Where(d => d.IsReady && d.TotalSize > 0)
                .Select(d => {
                    var totalGB = d.TotalSize / (1024.0 * 1024 * 1024);
                    var usedGB = (d.TotalSize - d.TotalFreeSpace) / (1024.0 * 1024 * 1024);
                    var percentage = (double)(d.TotalSize - d.TotalFreeSpace) / d.TotalSize * 100;
                    
                    return new DiskUsageDto
                    {
                        Name = d.Name,
                        TotalSpaceGB = double.IsFinite(totalGB) ? Math.Round(totalGB, 2) : 0,
                        UsedSpaceGB = double.IsFinite(usedGB) ? Math.Round(usedGB, 2) : 0,
                        UsagePercentage = double.IsFinite(percentage) ? Math.Round(percentage, 2) : 0
                    };
                }).ToList();
        }

        private async Task<List<ProcessDto>> GetTopProcessesAsync(double totalMemoryBytes)
        {
            var processes = Process.GetProcesses()
                .Where(p => !p.HasExited)
                .OrderByDescending(p => p.WorkingSet64)
                .Take(10)
                .ToList();

            var tasks = processes.Select(async p => {
                try
                {
                    var memoryMB = p.WorkingSet64 / (1024.0 * 1024.0);
                    var memoryPercentage = totalMemoryBytes > 0 ? (double)p.WorkingSet64 / totalMemoryBytes * 100 : 0;
                    var cpuUsage = await GetProcessCpuUsage(p);
                    
                    return new ProcessDto
                    {
                        Id = p.Id,
                        Name = p.ProcessName,
                        MemoryUsageMB = double.IsFinite(memoryMB) ? Math.Round(memoryMB, 2) : 0,
                        MemoryUsagePercentage = double.IsFinite(memoryPercentage) ? Math.Round(memoryPercentage, 2) : 0,
                        CpuUsage = double.IsFinite(cpuUsage) ? cpuUsage : 0
                    };
                }
                catch
                {
                    return null;
                }
            });

            var results = await Task.WhenAll(tasks);
            return results.Where(r => r != null).ToList();
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
            catch
            {
                return 0;
            }
        }

        private List<ServiceDto> GetServices()
        {
            try
            {
                var services = new List<ServiceDto>();
                var processInfo = new ProcessStartInfo
                {
                    FileName = "systemctl",
                    Arguments = "list-units --type=service --state=running --no-pager --no-legend",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                if (process != null)
                {
                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                    {
                        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 4)
                        {
                            services.Add(new ServiceDto
                            {
                                Name = parts[0],
                                DisplayName = parts[0],
                                Status = parts[3]
                            });
                        }
                    }
                }

                return services;
            }
            catch
            {
                return new List<ServiceDto>();
            }
        }

        private string GetUptime()
        {
            try
            {
                var uptimeText = File.ReadAllText("/proc/uptime");
                var uptimeSeconds = double.Parse(uptimeText.Split(' ')[0]);
                var uptime = TimeSpan.FromSeconds(uptimeSeconds);
                return uptime.TotalDays >= 1 ? $"{uptime.Days} days, {uptime.Hours} hours" : $"{uptime.Hours} hours, {uptime.Minutes} minutes";
            }
            catch
            {
                return "N/A";
            }
        }

        private NetworkInfoMetricDto GetNetworkInfo()
        {
            long bytesSent = 0;
            long bytesReceived = 0;

            try
            {
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
            }
            catch { }

            return new NetworkInfoMetricDto
            {
                BytesIn = FormatBytes(bytesReceived),
                BytesOut = FormatBytes(bytesSent)
            };
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