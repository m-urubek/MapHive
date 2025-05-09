using AutoMapper;
using MapHive.Models.Enums;
using MapHive.Models.RepositoryModels;
using MapHive.Repositories;
using MapHive.Singletons;
using Newtonsoft.Json;
using System.Text;

namespace MapHive.Services
{
    public class LogManagerSingleton : ILogManagerSingleton
    {
        private readonly IFileLoggerSingleton _fileLogger;
        private readonly ILogRepository _logRepository;

        public LogManagerSingleton(IFileLoggerSingleton fileLogger,
            ILogRepository logRepository)
        {
            this._fileLogger = fileLogger;
            this._logRepository = logRepository;
        }

        public void Log(
            LogSeverity severity,
            string message,
            Exception? exception = null,
            string? source = null,
            string? additionalData = null,
            int? userId = null,
            string? requestPath = null)
        {
            new Thread(async () =>
            {
                Thread.CurrentThread.IsBackground = true;
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
                        AdditionalData = this.TryFormatAdditionalData(additionalData),
                        Timestamp = DateTime.UtcNow
                    };

                    try
                    {
                        this._fileLogger.LogToFile(logCreate.ToString());
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to log to file {ex.ToString()}");
                        Console.WriteLine(logCreate.ToString());
                    }
                    try
                    {
                        _ = await this._logRepository.CreateLogRowAsync(logCreate);
                    }
                    catch (Exception ex)
                    {
                        this._fileLogger.LogToFile("Failed to log to database: " + ex.ToString());
                    }
                }
                catch (Exception ex)
                {
                    try
                    {
                        _ = await this._logRepository.CreateLogRowAsync(new LogCreate()
                        {
                            Message = "Failed to log: " + ex.ToString(),
                            Severity = LogSeverity.Critical,
                            Timestamp = DateTime.Now
                        });
                    }
                    catch { /*ignore*/ }
                    try
                    {
                        this._fileLogger.LogToFile("Failed to log: " + ex.ToString());
                    }
                    catch { /*ignore*/ }
                    Console.WriteLine($"Failed to log: {ex.ToString()}");
                }
            }).Start();
        }

        private string? TryFormatAdditionalData(string? data)
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                return null;
            }

            try
            {
                return data.TrimStart().StartsWith("{") || data.TrimStart().StartsWith("[")
                    ? data
                    : JsonConvert.SerializeObject(new { data });
            }
            catch
            {
                return JsonConvert.SerializeObject(new { error = "format", data });
            }
        }
    }
}