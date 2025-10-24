namespace EvvaAgent.Domain
{
    public class Log
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string? Message { get; set; }
        public string? Level { get; set; }
        public string? Uuid { get; set; }
        public string? JsonData { get; set; }
    }
}