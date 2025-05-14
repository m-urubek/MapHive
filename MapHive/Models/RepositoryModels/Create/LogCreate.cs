namespace MapHive.Models.RepositoryModels
{
    using System.Text;
    using MapHive.Models.Enums;
    using MapHive.Utilities.Extensions;

    public class LogCreate
    {
        public required DateTime Timestamp { get; set; }
        public required LogSeverity Severity { get; set; }
        public required string Message { get; set; }
        public string? Source { get; set; }
        public Exception? Exception { get; set; }
        public int? AccountId { get; set; }
        public string? RequestPath { get; set; }
        public string? AdditionalData { get; set; }

        public override string ToString()
        {
            try
            {
                StringBuilder sb = new();

                _ = sb.AppendLine(handler: $"SEVERITY: {Severity}");
                _ = sb.AppendLine(handler: $"MESSAGE: {Message}");
                sb.AppendLineIfNotNullOrWhitespace(value: $"SOURCE: {Source}");
                _ = sb.AppendLine(handler: $"USER: {AccountId?.ToString() ?? "system"}");
                sb.AppendLineIfNotNullOrWhitespace(value: $"SOURCE: {RequestPath}");
                sb.AppendLineIfNotNullOrWhitespace(value: $"EXCEPTION: {Exception?.ToString()}");
                sb.AppendLineIfNotNullOrWhitespace(value: $"ADDITIONAL: {AdditionalData}");
                _ = sb.AppendLine(value: new string('-', 80));

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"[LogCreate ToString Error] {ex}";
            }
        }
    }
}