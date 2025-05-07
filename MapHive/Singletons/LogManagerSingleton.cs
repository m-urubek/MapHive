using MapHive.Models.Enums;
using MapHive.Models.RepositoryModels;
using MapHive.Repositories;
using Newtonsoft.Json;
using System.Text;

namespace MapHive.Singletons
{
    public class LogManagerSingleton : ILogManagerSingleton
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IFileLoggerSingleton _fileLogger;
        private readonly ILogRepository _logRepository;

        public LogManagerSingleton(IHttpContextAccessor httpContextAccessor, IFileLoggerSingleton fileLogger, ILogRepository logRepository)
        {
            this._httpContextAccessor = httpContextAccessor;
            this._fileLogger = fileLogger;
            this._logRepository = logRepository;
        }

        public void Information(string message, string? source = null, Exception? exception = null, string? additionalData = null)
        {
            this.WriteLog(new()
            {
                Exception = exception,
                Message = message,
                Severity = LogSeverity.Information,
                Source = source,
                AdditionalData = additionalData
            });
        }

        public void Warning(string message, string? source = null, Exception? exception = null, string? additionalData = null)
        {
            this.WriteLog(new()
            {
                Exception = exception,
                Message = message,
                Severity = LogSeverity.Warning,
                Source = source,
                AdditionalData = additionalData
            });
        }

        public void Error(string message, string? source = null, Exception? exception = null, string? additionalData = null)
        {
            this.WriteLog(new()
            {
                Exception = exception,
                Message = message,
                Severity = LogSeverity.Error,
                Source = source,
                AdditionalData = additionalData
            });
        }

        public void Critical(string message, string? source = null, Exception? exception = null, string? additionalData = null)
        {
            this.WriteLog(new()
            {
                Exception = exception,
                Message = message,
                AdditionalData = additionalData,
                Severity = LogSeverity.Critical,
                Source = source
            });
        }

        private string GetCurrentUsername()
        {
            // Try to get username from claims or Identity
            System.Security.Claims.ClaimsPrincipal? user = this._httpContextAccessor.HttpContext?.User;
            return user?.Identity?.IsAuthenticated == true ? user?.Identity?.Name ?? "system" : "guest";
        }

        private int? GetCurrentUserId()
        {
            // Try to get user ID from claims
            System.Security.Claims.ClaimsPrincipal? user = this._httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                string? userIdClaim = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
                {
                    return userId;
                }
            }
            return null;
        }

        private string? GetCurrentPath()
        {
            return this._httpContextAccessor.HttpContext?.Request?.Path.ToString() ?? "system";
        }

        private void WriteLog(LogWrite logWrite)
        {
            string capturedUser = this.GetCurrentUsername();
            int? capturedUserId = this.GetCurrentUserId();
            string? capturedPath = this.GetCurrentPath();
            string? formattedData = this.TryFormatAdditionalData(logWrite.AdditionalData);

            new Thread(async () =>
            {
                Thread.CurrentThread.IsBackground = true;
                try
                {
                    _ = await this._logRepository.CreateLogRowAsync(new()
                    {
                        AdditionalData = formattedData,
                        RequestPath = capturedPath,
                        UserId = capturedUserId,
                        Severity = logWrite.Severity,
                        Timestamp = DateTime.UtcNow,
                        Exception = logWrite.Exception,
                        Message = logWrite.Message,
                        Source = logWrite.Source
                    });
                    this.WriteToFileInternal(logWrite.Severity, logWrite.Message, logWrite.Source, logWrite.Exception, formattedData, capturedUser, capturedPath);
                }
                catch (Exception dbEx)
                {
                    Console.WriteLine($"[LogGet DB Error] {dbEx}");
                    Console.WriteLine($"[LogGet DB Error] Original: Severity={logWrite.Severity}, Msg={logWrite.Message}");
                    string fallback = $"DB failed: {dbEx.Message}. Data: {formattedData}";
                    this.WriteToFileInternal(logWrite.Severity, logWrite.Message, logWrite.Source, logWrite.Exception, fallback, capturedUser, capturedPath);
                }
            }).Start();
        }

        private void WriteToFileInternal(LogSeverity severity, string message, string? source, Exception? exception, string? additionalData, string? userName, string? requestPath)
        {
            try
            {
                StringBuilder sb = new StringBuilder()
                    .AppendLine($"SEVERITY: {severity}")
                    .AppendLine($"MESSAGE: {message}");
                if (!string.IsNullOrEmpty(source))
                {
                    _ = sb.AppendLine($"SOURCE: {source}");
                }

                if (!string.IsNullOrEmpty(userName))
                {
                    _ = sb.AppendLine($"USER: {userName}");
                }

                if (!string.IsNullOrEmpty(requestPath))
                {
                    _ = sb.AppendLine($"PATH: {requestPath}");
                }

                if (exception != null)
                {
                    _ = sb.AppendLine($"EXCEPTION: {exception}");
                }

                if (!string.IsNullOrEmpty(additionalData))
                {
                    _ = sb.AppendLine($"ADDITIONAL: {additionalData}");
                }

                _ = sb.AppendLine(new string('-', 80));
                this._fileLogger.LogToFile(sb.ToString());
            }
            catch { /* ignore */ }
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

        public void Log(LogSeverity severity, string message, string? source = null, Exception? exception = null, string? additionalData = null)
        {
            switch (severity)
            {
                case LogSeverity.Information: this.Information(message, source, exception, additionalData); break;
                case LogSeverity.Warning: this.Warning(message, source, exception, additionalData); break;
                case LogSeverity.Error: this.Error(message, source, exception, additionalData); break;
                case LogSeverity.Critical: this.Critical(message, source, exception, additionalData); break;
                default: this.Warning(message, source, exception, additionalData); break;
            }
        }
    }
}