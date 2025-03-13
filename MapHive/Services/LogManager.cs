using MapHive.Models;
using Newtonsoft.Json;
using System.Data.SQLite;

namespace MapHive.Services
{
    public class LogManager
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LogManager(IHttpContextAccessor httpContextAccessor)
        {
            this._httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Logs an informational message
        /// </summary>
        public void Information(string message, string? source = null, Exception? exception = null, string? additionalData = null)
        {
            this.WriteLog(LogSeverity.Information, message, source, exception, additionalData);
        }

        /// <summary>
        /// Logs a warning message
        /// </summary>
        public void Warning(string message, string? source = null, Exception? exception = null, string? additionalData = null)
        {
            this.WriteLog(LogSeverity.Warning, message, source, exception, additionalData);
        }

        /// <summary>
        /// Logs an error message
        /// </summary>
        public void Error(string message, string? source = null, Exception? exception = null, string? additionalData = null)
        {
            this.WriteLog(LogSeverity.Error, message, source, exception, additionalData);
        }

        /// <summary>
        /// Logs a critical error message
        /// </summary>
        public void Critical(string message, string? source = null, Exception? exception = null, string? additionalData = null)
        {
            this.WriteLog(LogSeverity.Critical, message, source, exception, additionalData);
        }

        /// <summary>
        /// Writes a log entry to the database
        /// </summary>
        private void WriteLog(int severityId, string message, string? source = null, Exception? exception = null, string? additionalData = null)
        {
            try
            {
                // Get current user and request path if available
                string? userName = null;
                string? requestPath = null;

                if (this._httpContextAccessor.HttpContext != null)
                {
                    userName = this._httpContextAccessor.HttpContext.User?.Identity?.Name;
                    requestPath = this._httpContextAccessor.HttpContext.Request?.Path;
                }

                // Prepare exception details
                string? exceptionMessage = exception?.Message;
                string? stackTrace = exception?.StackTrace;

                // Format additional data as JSON if it's not already
                if (additionalData != null && !additionalData.StartsWith("{") && !additionalData.StartsWith("["))
                {
                    additionalData = JsonConvert.SerializeObject(new { data = additionalData });
                }

                // Insert log into database
                string query = @"
                    INSERT INTO Logs 
                    (Timestamp, SeverityId, Message, Source, Exception, StackTrace, UserName, RequestPath, AdditionalData) 
                    VALUES 
                    (@Timestamp, @SeverityId, @Message, @Source, @Exception, @StackTrace, @UserName, @RequestPath, @AdditionalData)";

                SQLiteParameter[] parameters = new SQLiteParameter[]
                {
                    new("@Timestamp", DateTime.UtcNow.ToString("o")),
                    new("@SeverityId", severityId),
                    new("@Message", message),
                    new("@Source", source ?? ""),
                    new("@Exception", exceptionMessage ?? ""),
                    new("@StackTrace", stackTrace ?? ""),
                    new("@UserName", userName ?? ""),
                    new("@RequestPath", requestPath ?? ""),
                    new("@AdditionalData", additionalData ?? "")
                };

                _ = MainClient.SqlClient.Insert(query, parameters);
            }
            catch (Exception ex)
            {
                // If logging to the database fails, write to Console output as a fallback
                Console.WriteLine($"Failed to write log: {ex.Message}");
                Console.WriteLine($"Original log: Severity={severityId}, Message={message}");
            }
        }
    }
}