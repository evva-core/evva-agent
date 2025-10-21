namespace EvvaAgent.Modules.Nginx.Domain
{
    public class ReverseProxyConfig
    {
        public string Domain { get; set; } = string.Empty;
        public int Port { get; set; } = 80;
        public string TargetUrl { get; set; } = string.Empty;
    }

    public class StaticSiteConfig
    {
        public string Domain { get; set; } = string.Empty;
        public int Port { get; set; } = 80;
        public string RootPath { get; set; } = string.Empty;
        public string? IndexFiles { get; set; }
    }

    public class NginxServerConfig
    {
        public string ServerName { get; set; } = string.Empty;
        public int Port { get; set; } = 80;
        public string Root { get; set; } = string.Empty;
        public string Index { get; set; } = "index.html index.htm";
        public bool Enabled { get; set; } = true;
        public List<NginxLocation> Locations { get; set; } = new();
        public List<string> CustomDirectives { get; set; } = new();
    }

    public class NginxLocation
    {
        public string Path { get; set; } = "/";
        public string ProxyPass { get; set; } = string.Empty;
        public string TryFiles { get; set; } = string.Empty;
        public List<string> CustomDirectives { get; set; } = new();
    }

    public class NginxStatus
    {
        public bool IsRunning { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public int ProcessCount { get; set; }
        public List<NginxProcessInfo> Processes { get; set; } = new();
    }

    public class NginxProcessInfo
    {
        public int Id { get; set; }
        public DateTime StartTime { get; set; }
        public long WorkingSet { get; set; }
    }
}