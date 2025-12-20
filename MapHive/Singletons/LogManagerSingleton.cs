namespace MapHive.Services;

using MapHive.Models.Enums;
using MapHive.Singletons;
using Serilog;
using Serilog.Events;

public class LogManagerSingleton(IFileLoggerSingleton fileLogger) : ILogManagerSingleton
{
    private readonly IFileLoggerSingleton _fileLogger = fileLogger;

    public Task<int> LogAsync(
        LogSeverity severity,
        string message,
        Exception? exception = null,
        string? source = null,
        string? additionalData = null,
        int? accountId = null,
        string? requestPath = null)
    {
        try
        {
            LogEventLevel serilogLevel = MapSeverityToSerilog(severity: severity);

            // Create structured log with all context
            Log.ForContext("Source", source ?? "Unknown")
               .ForContext("AccountId", accountId)
               .ForContext("RequestPath", requestPath ?? "N/A")
               .ForContext("AdditionalData", additionalData)
               .ForContext("Severity", severity.ToString())
               .Write(level: serilogLevel, exception: exception, messageTemplate: message);
        }
        catch (Exception ex)
        {
            // Fallback to file logging if Serilog fails
            try
            {
                _fileLogger.LogToFile($"Serilog failed, fallback log: {message} | Exception: {exception} | Error: {ex}");
            }
            catch
            {
                Console.WriteLine($"Failed to log: {ex}");
            }
        }

        // Return 0 since we no longer generate SQL IDs
        return Task.FromResult(result: 0);
    }

    private static LogEventLevel MapSeverityToSerilog(LogSeverity severity)
    {
        return severity switch
        {
            LogSeverity.Information => LogEventLevel.Information,
            LogSeverity.Warning => LogEventLevel.Warning,
            LogSeverity.Error => LogEventLevel.Error,
            LogSeverity.Critical => LogEventLevel.Fatal,
            _ => LogEventLevel.Information
        };
    }
}
