using Microsoft.EntityFrameworkCore;
using EvvaAgent.Data;
using EvvaAgent.Domain;
using EvvaAgent.Domain.Repositories;

namespace EvvaAgent.Infrastructure.Data
{
    public class ConfigurationRepository : IConfigurationRepository
    {
        private readonly AgentDbContext _context;

        public ConfigurationRepository(AgentDbContext context)
        {
            _context = context;
        }

        public async Task<Configuration?> GetAsync()
        {
            return await _context.Configurations.FirstOrDefaultAsync();
        }

        public async Task SaveAsync(Configuration configuration)
        {
            var existing = await _context.Configurations.FirstOrDefaultAsync();
            if (existing != null)
            {
                existing.FirstRun = configuration.FirstRun;
                existing.AdminServerUrl = configuration.AdminServerUrl;
                existing.Token = configuration.Token;
                existing.NginxPath = configuration.NginxPath;
                existing.NginxConfigPath = configuration.NginxConfigPath;
                _context.Configurations.Update(existing);
            }
            else
            {
                configuration.Id = 1;
                _context.Configurations.Add(configuration);
            }
            await _context.SaveChangesAsync();
        }
    }
}