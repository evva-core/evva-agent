namespace EvvaAgent.Domain
{
    public class Project
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Status { get; set; }
        public string? Url { get; set; }
        public string? Uuid { get; set; }
        public string? Path { get; set; }
        public bool AutoSync { get; set; }
        public string? Branch { get; set; }
        public string? Service { get; set; }
    }
}