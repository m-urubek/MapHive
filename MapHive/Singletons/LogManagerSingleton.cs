namespace MapHive.Services;

using System.Text;

using MapHive.Models.Enums;
using MapHive.Repositories;
using MapHive.Singletons;
using Newtonsoft.Json;

public class LogManagerSingleton(IFileLoggerSingleton fileLogger,
    ILogRepository logRepository) : ILogManagerSingleton
{
    private readonly IFileLoggerSingleton _fileLogger = fileLogger;
    private readonly ILogRepository _logRepository = logRepository;

    public async Task<int> LogAsync(
        LogSeverity severity,
        string message,
        Exception? exception = null,
        string? source = null,
        string? additionalData = null,
        int? accountId = null,
        string? requestPath = null)
    {
        int logId = 0;
        try
        {
            StringBuilder sb = new();
            _ = sb.AppendLine(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
            _ = sb.AppendLine(handler: $"SEVERITY: {severity}");
            _ = sb.AppendLine(handler: $"MESSAGE: {message}");
            sb.AppendLineIfNotNullOrWhitespace(value: $"SOURCE: {source}");
            _ = sb.AppendLine(handler: $"USER: {accountId?.ToString() ?? "system"}");
            sb.AppendLineIfNotNullOrWhitespace(value: $"SOURCE: {requestPath}");
            sb.AppendLineIfNotNullOrWhitespace(value: $"EXCEPTION: {exception?.ToString()}");
            sb.AppendLineIfNotNullOrWhitespace(value: $"ADDITIONAL: {TryFormatAdditionalData(data: additionalData)}");
            _ = sb.AppendLine(value: new string('-', 80));

            try
            {
                _fileLogger.LogToFile(sb.ToString());
            }
            catch (Exception exFile)
            {
                Console.WriteLine($"Failed to log to file {exFile}");
                Console.WriteLine(sb);
            }
            try
            {
                logId = await _logRepository.CreateLogRowAsync(
                    timestamp: DateTime.UtcNow,
                    severity: severity,
                    message: message,
                    source: source,
                    exception: exception,
                    accountId: accountId,
                    requestPath: requestPath,
                    additionalData: additionalData);
            }
            catch (Exception exDb)
            {
                _fileLogger.LogToFile("Failed to log to database: " + exDb);
            }
        }
        catch (Exception ex)
        {
            try
            {
                logId = await _logRepository.CreateLogRowAsync(
                    timestamp: DateTime.UtcNow,
                    severity: LogSeverity.Critical,
                    message: "Failed to log: " + ex,
                    source: null,
                    exception: ex,
                    accountId: null,
                    requestPath: null,
                    additionalData: null);
            }
            catch { }
            try
            {
                _fileLogger.LogToFile("Failed to log: " + ex);
            }
            catch { }
            Console.WriteLine($"Failed to log: {ex}");
        }
        return logId;
    }

    private static string? TryFormatAdditionalData(string? data)
    {
        if (string.IsNullOrWhiteSpace(value: data))
        {
            return null;
        }

        try
        {
            return data.TrimStart().StartsWith(value: '{') || data.TrimStart().StartsWith(value: '[')
                ? data
                : JsonConvert.SerializeObject(value: new { data });
        }
        catch
        {
            return JsonConvert.SerializeObject(value: new { error = "format", data });
        }
    }
}
