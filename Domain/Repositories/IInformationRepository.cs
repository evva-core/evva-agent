namespace EvvaAgent.Domain.Repositories
{
    public interface IInformationRepository
    {
        Task<Information?> GetAsync();
        Task SaveAsync(Information information);
    }
}