using Microsoft.AspNetCore.SignalR.Client;

namespace EvvaAgent.Infrastructure.Communication
{
    public class ConnectionHealthService : BackgroundService
    {
        private readonly ICoreHubService _coreHubService;
        private readonly ILogger<ConnectionHealthService> _logger;

        public ConnectionHealthService(ICoreHubService coreHubService, ILogger<ConnectionHealthService> logger)
        {
            _coreHubService = coreHubService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (!_coreHubService.IsConnected)
                    {
                        _logger.LogWarning("Connection to core lost, attempting to reconnect...");
                        await _coreHubService.StartAsync();
                    }
                    
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in connection health check");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
        }
    }
}