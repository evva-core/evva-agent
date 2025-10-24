using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EvvaAgent.Data
{
    public class AgentDbContextFactory : IDesignTimeDbContextFactory<AgentDbContext>
    {
        public AgentDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AgentDbContext>();
            optionsBuilder.UseSqlite("Data Source=Data/EvvaAgent.db");
            
            return new AgentDbContext(optionsBuilder.Options);
        }
    }
}