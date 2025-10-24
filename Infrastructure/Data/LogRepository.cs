using Microsoft.EntityFrameworkCore;
using EvvaAgent.Data;
using EvvaAgent.Domain;
using EvvaAgent.Domain.Repositories;

namespace EvvaAgent.Infrastructure.Data
{
    public class LogRepository : ILogRepository
    {
        private readonly AgentDbContext _context;

        public LogRepository(AgentDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Log>> GetAllAsync()
        {
            return await _context.Logs.OrderByDescending(l => l.Date).ToListAsync();
        }

        public async Task<Log?> GetByIdAsync(int id)
        {
            return await _context.Logs.FindAsync(id);
        }

        public async Task SaveAsync(Log log)
        {
            if (log.Id == 0)
            {
                _context.Logs.Add(log);
            }
            else
            {
                _context.Logs.Update(log);
            }
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var log = await _context.Logs.FindAsync(id);
            if (log != null)
            {
                _context.Logs.Remove(log);
                await _context.SaveChangesAsync();
            }
        }
    }
}