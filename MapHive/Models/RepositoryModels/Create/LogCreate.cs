using MapHive.Models.Enums;
using MapHive.Utilities.Extensions;
using System.Text;

namespace MapHive.Models.RepositoryModels
{
    public class LogCreate
    {
        public required DateTime Timestamp { get; set; }
        public required LogSeverity Severity { get; set; }
        public required string Message { get; set; }
        public string? Source { get; set; }
        public Exception? Exception { get; set; }
        public int? UserId { get; set; }
        public string? RequestPath { get; set; }
        public string? AdditionalData { get; set; }

        public override string ToString()
        {
            try
            {
                StringBuilder sb = new();

                _ = sb.AppendLine($"SEVERITY: {this.Severity}");
                _ = sb.AppendLine($"MESSAGE: {this.Message}");
                sb.AppendLineIfNotNullOrWhitespace($"SOURCE: {this.Source}");
                _ = sb.AppendLine($"USER: {this.UserId?.ToString() ?? "system"}");
                sb.AppendLineIfNotNullOrWhitespace($"SOURCE: {this.RequestPath}");
                sb.AppendLineIfNotNullOrWhitespace($"EXCEPTION: {this.Exception?.ToString()}");
                sb.AppendLineIfNotNullOrWhitespace($"ADDITIONAL: {this.AdditionalData}");
                _ = sb.AppendLine(new string('-', 80));

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"[LogCreate ToString Error] {ex}";
            }
        }
    }
}