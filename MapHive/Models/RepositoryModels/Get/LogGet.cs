namespace MapHive.Models.RepositoryModels
{
    using MapHive.Models.Enums;

    public class LogGet
    {
        public required LogSeverity Severity { get; set; }
        public required int Id_Log { get; set; }
        public required DateTime Timestamp { get; set; }
        public required string Message { get; set; }
        public string? Source { get; set; }
        public string? Exception { get; set; }
        public int? UserId { get; set; }
        public string? RequestPath { get; set; }
        public string? AdditionalData { get; set; }
    }
}