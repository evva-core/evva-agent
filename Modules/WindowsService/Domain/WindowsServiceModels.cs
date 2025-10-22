namespace EvvaAgent.Modules.WindowsService.Domain
{
    public class ServiceConfig
    {
        public string ServiceName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string ExecutablePath { get; set; } = string.Empty;
        public string? Description { get; set; }
        public ServiceStartType StartType { get; set; } = ServiceStartType.Manual;
        public string? Username { get; set; }
        public string? Password { get; set; }
    }

    public class ServiceOperationResult
    {
        public bool Success { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Output { get; set; }
        public string? Error { get; set; }
    }

    public class ServiceStatus
    {
        public string ServiceName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public ServiceStartType StartType { get; set; }
        public bool IsRunning { get; set; }
        public string? Description { get; set; }
    }

    public enum ServiceStartType
    {
        Automatic,
        Manual,
        Disabled
    }
}