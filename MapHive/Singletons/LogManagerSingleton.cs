namespace MapHive.Services
{
    using MapHive.Models.Enums;
    using MapHive.Models.RepositoryModels;
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
                LogCreate logCreate = new()
                {
                    Exception = exception,
                    Message = message,
                    Severity = severity,
                    Source = source,
                    AccountId = accountId,
                    RequestPath = requestPath,
                    AdditionalData = TryFormatAdditionalData(data: additionalData),
                    Timestamp = DateTime.UtcNow
                };
                try
                {
                    _fileLogger.LogToFile(logCreate.ToString());
                }
                catch (Exception exFile)
                {
                    Console.WriteLine($"Failed to log to file {exFile}");
                    Console.WriteLine(logCreate);
                }
                try
                {
                    logId = await _logRepository.CreateLogRowAsync(logCreate: logCreate);
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
                    logId = await _logRepository.CreateLogRowAsync(logCreate: new LogCreate
                    {
                        Message = "Failed to log: " + ex,
                        Severity = LogSeverity.Critical,
                        Timestamp = DateTime.Now
                    });
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
}
