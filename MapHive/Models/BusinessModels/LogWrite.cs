using MapHive.Models.Enums;

namespace MapHive.Models.RepositoryModels
{
    public class LogWrite
    {
        public LogSeverity Severity { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Source { get; set; }
        public required Exception Exception { get; set; }
        public string? AdditionalData { get; set; }
    }
}