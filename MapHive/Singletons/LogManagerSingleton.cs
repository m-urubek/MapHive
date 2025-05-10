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

        public void Log(
            LogSeverity severity,
            string message,
            Exception? exception = null,
            string? source = null,
            string? additionalData = null,
            int? userId = null,
            string? requestPath = null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    LogCreate logCreate = new()
                    {
                        Exception = exception,
                        Message = message,
                        Severity = severity,
                        Source = source,
                        UserId = userId,
                        RequestPath = requestPath,
                        AdditionalData = TryFormatAdditionalData(data: additionalData),
                        Timestamp = DateTime.UtcNow
                    };

                    try
                    {
                        _fileLogger.LogToFile(message: logCreate.ToString());
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(value: $"Failed to log to file {ex.ToString()}");
                        Console.WriteLine(value: logCreate.ToString());
                    }
                    try
                    {
                        _ = await _logRepository.CreateLogRowAsync(logCreate: logCreate);
                    }
                    catch (Exception ex)
                    {
                        _fileLogger.LogToFile(message: "Failed to log to database: " + ex.ToString());
                    }
                }
                catch (Exception ex)
                {
                    try
                    {
                        _ = await _logRepository.CreateLogRowAsync(logCreate: new LogCreate()
                        {
                            Message = "Failed to log: " + ex.ToString(),
                            Severity = LogSeverity.Critical,
                            Timestamp = DateTime.Now
                        });
                    }
                    catch { /*ignore*/ }
                    try
                    {
                        _fileLogger.LogToFile(message: "Failed to log: " + ex.ToString());
                    }
                    catch { /*ignore*/ }
                    Console.WriteLine(value: $"Failed to log: {ex.ToString()}");
                }
            });
        }

        private static string? TryFormatAdditionalData(string? data)
        {
            if (string.IsNullOrWhiteSpace(value: data))
            {
                return null;
            }

            try
            {
                return data.TrimStart().StartsWith(value: "{") || data.TrimStart().StartsWith(value: "[")
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