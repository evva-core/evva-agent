using EvvaAgent.DTOs;
using System.Threading.Tasks;

namespace EvvaAgent.Infrastructure.Metrics
{
    public interface IlinuxMetricsService
    {
        Task<HostMetricsDto> GetHostMetricsAsync();
    }
}