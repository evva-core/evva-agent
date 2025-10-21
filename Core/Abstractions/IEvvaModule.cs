namespace EvvaAgent.Core.Abstractions
{
    public interface IEvvaModule
    {
        string Name { get; }
        IEnumerable<string> GetAvailableCommands();
        Task<object> ExecuteCommandAsync(string command, string? parameters, IServiceProvider serviceProvider);
    }

    public interface IEvvaResource
    {
        Task<object> ExecuteAsync(string method, string? parameters, IServiceProvider serviceProvider);
        IEnumerable<string> GetAvailableMethods();
    }
}