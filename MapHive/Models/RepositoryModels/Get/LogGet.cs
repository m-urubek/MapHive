using MapHive.Models.Enums;

namespace MapHive.Models.RepositoryModels
{
    public class LogGet
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public int SeverityId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Source { get; set; }
        public string? Exception { get; set; }
        public string? UserName { get; set; }
        public string? RequestPath { get; set; }
        public string? AdditionalData { get; set; }
        public LogSeverity Severity { get; set; }
    }
}