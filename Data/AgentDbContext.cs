using Microsoft.EntityFrameworkCore;
using EvvaAgent.Domain;

namespace EvvaAgent.Data
{
    /// <summary>
    /// Represents the database context for the agent, using SQLite.
    /// It can be used to store agent-specific data, such as logs or configuration.
    /// </summary>
    public class AgentDbContext : DbContext
    {
        public AgentDbContext(DbContextOptions<AgentDbContext> options) : base(options) { }

        // Add DbSets for your domain models here if needed.
        // For example:
        // public DbSet<DeploymentLog> DeploymentLogs { get; set; }
    }
}
