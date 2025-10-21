namespace EvvaAgent.Modules.Nginx.Domain
{
    public class NginxConfiguration
    {
        public string? NginxPath { get; set; }
        public string? NginxConfigPath { get; set; }
    }

    public class NginxProject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Url { get; set; }
        public string Uuid { get; set; } = string.Empty;
        public string? Path { get; set; }
        public bool AutoSync { get; set; }
        public string? Branch { get; set; }
        public string? Service { get; set; }
    }
}