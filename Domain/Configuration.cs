namespace EvvaAgent.Domain
{
    public class Configuration
    {
        public int Id { get; set; }
        public int? FirstRun { get; set; }
        public string? AdminServerUrl { get; set; }
        public string? Token { get; set; }
        public string? NginxPath { get; set; }
        public string? NginxConfigPath { get; set; }
    }
}