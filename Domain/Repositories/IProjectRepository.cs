namespace EvvaAgent.Domain.Repositories
{
    public interface IProjectRepository
    {
        Task<IEnumerable<Project>> GetAllAsync();
        Task<Project?> GetByIdAsync(int id);
        Task<Project?> GetByNameAsync(string name);
        Task SaveAsync(Project project);
        Task DeleteAsync(int id);
        Task DeleteByNameAsync(string name);
    }
}