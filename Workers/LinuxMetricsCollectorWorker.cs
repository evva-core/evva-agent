using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using EvvaAgent.DTOs;
using EvvaAgent.Infrastructure.Metrics;
using EvvaAgent.Infrastructure.Communication; // Import the DTOs namespace

namespace EvvaAgent.Workers
{
    /// <summary>
    /// A background service that periodically collects host metrics 
    /// and sends them to the management server.
    /// </summary>
    public class LinuxMetricsCollectorWorker : BackgroundService
    {
        private readonly ILogger<MetricsCollectorWorker> _logger;
     
        private readonly ICoreHubService _coreHub;
        private readonly IlinuxMetricsService _linuxMetricsService;
        

        public LinuxMetricsCollectorWorker(ILogger<MetricsCollectorWorker> logger,ICoreHubService coreHub, IlinuxMetricsService ilinuxMetricsService)
        {
            _logger = logger;

            _coreHub = coreHub;
            _linuxMetricsService = ilinuxMetricsService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Checking connection status: {IsConnected}", _coreHub.IsConnected);
                    if (_coreHub.IsConnected)
                    {
                        _logger.LogInformation("Collecting metrics...");
                        var metrics = await _linuxMetricsService.GetHostMetricsAsync();

                        _logger.LogInformation("Sending metrics to core...");
                        var sent = await _coreHub.SendMetricsAsync(metrics);

                        if (sent)
                            _logger.LogInformation("Metrics sent successfully");
                        else
                            _logger.LogWarning("Failed to send metrics");
                    }
                    else
                    {
                        _logger.LogWarning("Not connected to core server - skipping metrics");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in metrics collection cycle");
                }

                await Task.Delay(5000, stoppingToken);
            }
        }


    }
}