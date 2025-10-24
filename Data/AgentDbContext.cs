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

        public DbSet<CollectMetric> CollectMetrics { get; set; }
        public DbSet<Configuration> Configurations { get; set; }
        public DbSet<Information> Information { get; set; }
        public DbSet<Log> Logs { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<RepositoryInfo> RepositoryInfos { get; set; }
        public DbSet<WorkflowStep> WorkflowSteps { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure entity relationships and constraints
            // modelBuilder.Entity<DeploymentRequest>()
            //     .HasMany(d => d.Repositories)
            //     .WithOne()
            //     .HasForeignKey("DeploymentRequestId")
            //     .OnDelete(DeleteBehavior.Cascade);

            // modelBuilder.Entity<DeploymentRequest>()
            //     .HasMany(d => d.WorkflowSteps)
            //     .WithOne()
            //     .HasForeignKey("DeploymentRequestId")
            //     .OnDelete(DeleteBehavior.Cascade);

            // Configure single instance entities
            modelBuilder.Entity<Configuration>()
                .HasData(new Configuration { Id = 1 });

            modelBuilder.Entity<Information>()
                .HasData(new Information { Id = 1, HostName = "", Os = "", Arch = "", Uuid = "" });
        }
    }
}
