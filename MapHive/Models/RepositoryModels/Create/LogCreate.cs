using MapHive.Models.Enums;

namespace MapHive.Models.RepositoryModels
{
    public class LogCreate
    {
        public DateTime Timestamp { get; set; }
        public LogSeverity Severity { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Source { get; set; }
        public Exception? Exception { get; set; }
        public int? UserId { get; set; }
        public string? RequestPath { get; set; }
        public string? AdditionalData { get; set; }
    }
}