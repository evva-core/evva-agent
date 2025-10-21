namespace EvvaAgent.Domain.Repositories
{
    public interface ILogRepository
    {
        Task<IEnumerable<Log>> GetAllAsync();
        Task<Log?> GetByIdAsync(int id);
        Task SaveAsync(Log log);
        Task DeleteAsync(int id);
    }
}