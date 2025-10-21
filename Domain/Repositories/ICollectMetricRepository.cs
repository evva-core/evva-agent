namespace EvvaAgent.Domain.Repositories
{
    public interface ICollectMetricRepository
    {
        Task<IEnumerable<CollectMetric>> GetAllAsync();
        Task<CollectMetric?> GetLatestAsync();
        Task SaveAsync(CollectMetric metric);
        Task DeleteOldAsync(DateTime cutoffDate);
    }
}