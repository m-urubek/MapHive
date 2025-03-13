namespace MapHive.Models
{
    public class Log
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public int SeverityId { get; set; }
        public required string Message { get; set; }
        public required string Source { get; set; }
        public required string Exception { get; set; }
        public required string UserName { get; set; }
        public required string RequestPath { get; set; }
        public required string AdditionalData { get; set; }

        // Navigation property
        public required LogSeverity Severity { get; set; }
    }
}