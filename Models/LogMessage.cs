namespace LogFetcher.Models
{
    public class LogMessage
    {
        public string? Level { get; set; }
        public string? Message { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}
