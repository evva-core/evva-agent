using Microsoft.EntityFrameworkCore;
using EvvaAgent.Data;
using EvvaAgent.Domain;
using EvvaAgent.Domain.Repositories;

namespace EvvaAgent.Infrastructure.Data
{
    public class CollectMetricRepository : ICollectMetricRepository
    {
        private readonly AgentDbContext _context;

        public CollectMetricRepository(AgentDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CollectMetric>> GetAllAsync()
        {
            return await _context.CollectMetrics.ToListAsync();
        }

        public async Task<CollectMetric?> GetLatestAsync()
        {
            return await _context.CollectMetrics
                .OrderByDescending(m => m.Date)
                .FirstOrDefaultAsync();
        }

        public async Task SaveAsync(CollectMetric metric)
        {
            if (metric.Id == 0)
            {
                _context.CollectMetrics.Add(metric);
            }
            else
            {
                _context.CollectMetrics.Update(metric);
            }
            await _context.SaveChangesAsync();
        }

        public async Task DeleteOldAsync(DateTime cutoffDate)
        {
            var oldMetrics = await _context.CollectMetrics
                .Where(m => m.Date < cutoffDate)
                .ToListAsync();

            _context.CollectMetrics.RemoveRange(oldMetrics);
            await _context.SaveChangesAsync();
        }
    }
}