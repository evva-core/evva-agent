namespace EvvaAgent.Domain.Repositories
{
    public interface IConfigurationRepository
    {
        Task<Configuration?> GetAsync();
        Task SaveAsync(Configuration configuration);
    }
}