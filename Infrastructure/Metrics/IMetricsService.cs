using EvvaAgent.DTOs;
using System.Threading.Tasks;

namespace EvvaAgent.Infrastructure.Metrics
{
    public interface IMetricsService
    {
        Task<HostMetricsDto> GetHostMetricsAsync();
    }
}