using Microsoft.EntityFrameworkCore;
using EvvaAgent.Data;
using EvvaAgent.Domain;
using EvvaAgent.Domain.Repositories;

namespace EvvaAgent.Infrastructure.Data
{
    public class InformationRepository : IInformationRepository
    {
        private readonly AgentDbContext _context;

        public InformationRepository(AgentDbContext context)
        {
            _context = context;
        }

        public async Task<Information?> GetAsync()
        {
            return await _context.Information.FirstOrDefaultAsync();
        }

        public async Task SaveAsync(Information information)
        {
            var existing = await _context.Information.FirstOrDefaultAsync();
            if (existing != null)
            {
                existing.HostName = information.HostName;
                existing.IpAddress = information.IpAddress;
                existing.Os = information.Os;
                existing.Arch = information.Arch;
                existing.IsDockerHost = information.IsDockerHost;
                existing.Uuid = information.Uuid;
                _context.Information.Update(existing);
            }
            else
            {
                information.Id = 1;
                _context.Information.Add(information);
            }
            await _context.SaveChangesAsync();
        }
    }
}