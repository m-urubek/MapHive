using MapHive.Utilities;

namespace MapHive.Singletons
{
    /// <summary>
    /// Service for logging messages to files with rotation and cleanup.
    /// </summary>
    public class FileLoggerSingleton : IFileLoggerSingleton, IDisposable
    {
        private readonly object _lockObject = new();
        private string _currentLogFile;
        private DateTime _currentLogFileCreationTime;
        private const long MaxLogFileSize = 1024 * 1024; // 1MB
        private const int MaxLogFileAgeDays = 30;
        private readonly string _logDirectory;
        private readonly Timer _cleanupTimer;

        public FileLoggerSingleton()
        {
            this._logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

            // Ensure log directory exists
            if (!Directory.Exists(this._logDirectory))
            {
                _ = Directory.CreateDirectory(this._logDirectory);
            }

            // Initialize the current log file on startup
            this.InitializeCurrentLogFile();

            // Set up a timer to check for old log files once per day
            this._cleanupTimer = new Timer(this.CleanupOldLogFiles, null, TimeSpan.FromDays(1), TimeSpan.FromDays(1)); // Start after 1 day, repeat daily
        }

        /// <summary>
        /// Writes a log entry to the current log file.
        /// </summary>
        public void LogToFile(string message)
        {
            // Ensure we have a valid log file that's not too large
            this.EnsureLogFile();

            // Format the log entry with timestamp
            string formattedMessage = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}";

            // Write to the file using the thread-safe writer
            ThreadSafeFileWriter.Write(this._currentLogFile, formattedMessage);
        }

        /// <summary>
        /// Initializes the current log file.
        /// </summary>
        private void InitializeCurrentLogFile()
        {
            lock (this._lockObject)
            {
                this._currentLogFileCreationTime = DateTime.UtcNow;
                string timestamp = this._currentLogFileCreationTime.ToString("yyyyMMdd_HHmmss");
                this._currentLogFile = Path.Combine(this._logDirectory, $"log_{timestamp}.txt");
            }
        }

        /// <summary>
        /// Ensures the current log file exists and is not too large.
        /// </summary>
        private void EnsureLogFile()
        {
            if (string.IsNullOrEmpty(this._currentLogFile))
            {
                this.InitializeCurrentLogFile();
                return;
            }

            lock (this._lockObject)
            {
                // Check if the current log file exists and is not too large
                if (File.Exists(this._currentLogFile))
                {
                    FileInfo fileInfo = new(this._currentLogFile);
                    if (fileInfo.Length >= MaxLogFileSize)
                    {
                        // Create a new log file if the current one is too large
                        this.InitializeCurrentLogFile();
                    }
                }
                else
                {
                    // If file doesn't exist (e.g., manually deleted), create a new one
                    this.InitializeCurrentLogFile();
                }
            }
        }

        /// <summary>
        /// Cleans up log files older than the configured age.
        /// </summary>
        private void CleanupOldLogFiles(object? state) // state can be null
        {
            try
            {
                DateTime cutoffDate = DateTime.UtcNow.AddDays(-MaxLogFileAgeDays);
                string[] logFiles = Directory.GetFiles(this._logDirectory, "log_*.txt");

                foreach (string file in logFiles)
                {
                    // Get file info safely
                    FileInfo fileInfo;
                    try
                    {
                        fileInfo = new FileInfo(file);
                    }
                    catch (IOException)
                    {
                        // Skip files that might be locked or inaccessible
                        continue;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Skip files we don't have permission for
                        continue;
                    }

                    // Skip the current log file
                    lock (this._lockObject)
                    {
                        if (file == this._currentLogFile)
                        {
                            continue;
                        }
                    }

                    if (fileInfo.CreationTimeUtc < cutoffDate)
                    {
                        try
                        {
                            fileInfo.Delete();
                        }
                        catch (IOException)
                        {
                            // File might be in use, try again later
                        }
                        catch (UnauthorizedAccessException)
                        {
                            // Permissions issue, log or ignore
                        }
                    }
                }
            }
            catch (Exception ex) // Catch specific exceptions if possible
            {
                // LogGet cleanup failure? Avoid using LogToFile here to prevent recursion.
                // Consider logging to Debug output or a separate error file.
                System.Diagnostics.Debug.WriteLine($"Error during log cleanup: {ex.Message}");
            }
        }

        public void Dispose()
        {
            this._cleanupTimer?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}