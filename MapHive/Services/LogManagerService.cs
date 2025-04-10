using MapHive.Models;
using MapHive.Singletons;
using Newtonsoft.Json;
using System.Data.SQLite;
using System.Text;

namespace MapHive.Services
{
    public class LogManagerService
    {
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
            // Log to database
            this.WriteLog(LogSeverity.Error, message, source, exception, additionalData);

            // Also log errors to file
            this.WriteToFile(LogSeverity.Error, message, source, exception, additionalData);
        }

        /// <summary>
        /// Logs a critical error message
        /// </summary>
        public void Critical(string message, string? source = null, Exception? exception = null, string? additionalData = null)
        {
            // Log to database
            this.WriteLog(LogSeverity.Critical, message, source, exception, additionalData);

            // Also log critical errors to file
            this.WriteToFile(LogSeverity.Critical, message, source, exception, additionalData);
        }

        /// <summary>
        /// Writes a log entry to the database
        /// </summary>
        private void WriteLog(int severityId, string message, string? source = null, Exception? exception = null, string? additionalData = null)
        {
            // Capture HttpContext information before creating the background thread
            string? userName = null;
            string? requestPath = null;

            userName = CurrentRequest.IsAuthenticated ? CurrentRequest.Username : null;
            requestPath = CurrentRequest.RequestPath;

            // Format additional data as JSON if it's not already - do this on the current thread
            if (additionalData != null && !additionalData.StartsWith("{") && !additionalData.StartsWith("["))
            {
                additionalData = JsonConvert.SerializeObject(new { data = additionalData });
            }

            // Get a copy of all necessary data
            string capturedMessage = message;
            string? capturedSource = source;
            string? capturedException = exception?.ToString();
            string? capturedAdditionalData = additionalData;

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                try
                {
                    // Insert log into database using the captured values
                    string query = @"
                    INSERT INTO Logs 
                    (Timestamp, SeverityId, Message, Source, Exception, UserName, RequestPath, AdditionalData) 
                    VALUES 
                    (@Timestamp, @SeverityId, @Message, @Source, @Exception, @UserName, @RequestPath, @AdditionalData)";

                    SQLiteParameter[] parameters = new SQLiteParameter[]
                    {
                        new("@Timestamp", DateTime.UtcNow.ToString("o")),
                        new("@SeverityId", severityId),
                        new("@Message", capturedMessage),
                        new("@Source", capturedSource ?? ""),
                        new("@Exception", capturedException ?? ""),
                        new("@UserName", userName ?? ""),
                        new("@RequestPath", requestPath ?? ""),
                        new("@AdditionalData", capturedAdditionalData ?? "")
                    };

                    _ = CurrentRequest.SqlClient.Insert(query, parameters);
                }
                catch (Exception ex)
                {
                    // If logging to the database fails, write to Console output as a fallback
                    Console.WriteLine($"Failed to write log to database: {ex.ToString()}");
                    Console.WriteLine($"Original log: Severity={severityId}, Message={capturedMessage}");

                    // Try to write to file as a fallback
                    try
                    {
                        this.WriteToFile(severityId, capturedMessage, capturedSource, exception,
                            $"Database logging failed: {ex.ToString()}. {capturedAdditionalData}");
                    }
                    catch
                    {
                        // Last resort, if everything fails
                        Console.WriteLine("Failed to write to log file as well");
                    }
                }
            }).Start();
        }

        /// <summary>
        /// Writes a log entry to the log file
        /// </summary>
        private void WriteToFile(int severityId, string message, string? source = null,
            Exception? exception = null, string? additionalData = null)
        {
            try
            {
                StringBuilder logBuilder = new();

                // Get severity name
                string severityName = severityId switch
                {
                    1 => "Information",
                    2 => "Warning",
                    3 => "Error",
                    4 => "Critical",
                    _ => "Unknown"
                };

                // Build the log entry
                _ = logBuilder.AppendLine($"SEVERITY: {severityName}");
                _ = logBuilder.AppendLine($"MESSAGE: {message}");

                if (!string.IsNullOrEmpty(source))
                {
                    _ = logBuilder.AppendLine($"SOURCE: {source}");
                }

                // Get current user and request path if available
                string? userName = CurrentRequest.IsAuthenticated ? CurrentRequest.Username : null;
                string? requestPath = CurrentRequest.RequestPath;

                if (!string.IsNullOrEmpty(userName))
                {
                    _ = logBuilder.AppendLine($"USER: {userName}");
                }

                if (!string.IsNullOrEmpty(requestPath))
                {
                    _ = logBuilder.AppendLine($"PATH: {requestPath}");
                }

                // Add exception details if available
                if (exception != null)
                {
                    _ = logBuilder.AppendLine($"EXCEPTION: {exception.ToString()}");
                }

                // Add additional data if available
                if (!string.IsNullOrEmpty(additionalData))
                {
                    _ = logBuilder.AppendLine($"ADDITIONAL DATA: {additionalData}");
                }

                // Add separator for better readability
                _ = logBuilder.AppendLine(new string('-', 80));

                // Write to file
                FileLoggerService.LogToFile(logBuilder.ToString());
            }
            catch (Exception ex)
            {
                // If file logging fails, write to console as a last resort
                Console.WriteLine($"Failed to write log to file: {ex.ToString()}");
            }
        }
    }
}