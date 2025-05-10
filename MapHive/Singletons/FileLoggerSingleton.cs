namespace MapHive.Singletons
{
    using MapHive.Utilities;

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
        private bool _disposed;

        public FileLoggerSingleton()
        {
            _currentLogFile = "";
            _logDirectory = Path.Combine(path1: AppDomain.CurrentDomain.BaseDirectory, path2: "Logs");

            // Ensure log directory exists
            if (!Directory.Exists(path: _logDirectory))
            {
                _ = Directory.CreateDirectory(path: _logDirectory);
            }

            // Initialize the current log file on startup
            InitializeCurrentLogFile();

            // Set up a timer to check for old log files once per day
            _cleanupTimer = new Timer(CleanupOldLogFiles, null, TimeSpan.FromDays(value: 1), TimeSpan.FromDays(value: 1)); // Start after 1 day, repeat daily
        }

        /// <summary>
        /// Writes a log entry to the current log file.
        /// </summary>
        public void LogToFile(string message)
        {
            // Ensure we have a valid log file that's not too large
            EnsureLogFile();

            // Format the log entry with timestamp
            string formattedMessage = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}";

            // Write to the file using the thread-safe writer
            ThreadSafeFileWriter.Write(fileName: _currentLogFile, text: formattedMessage);
        }

        /// <summary>
        /// Initializes the current log file.
        /// </summary>
        private void InitializeCurrentLogFile()
        {
            lock (_lockObject)
            {
                _currentLogFileCreationTime = DateTime.UtcNow;
                string timestamp = _currentLogFileCreationTime.ToString(format: "yyyyMMdd_HHmmss");
                _currentLogFile = Path.Combine(path1: _logDirectory, path2: $"log_{timestamp}.txt");
            }
        }

        /// <summary>
        /// Ensures the current log file exists and is not too large.
        /// </summary>
        private void EnsureLogFile()
        {
            if (string.IsNullOrEmpty(value: _currentLogFile))
            {
                InitializeCurrentLogFile();
                return;
            }

            lock (_lockObject)
            {
                // Check if the current log file exists and is not too large
                if (File.Exists(path: _currentLogFile))
                {
                    FileInfo fileInfo = new(_currentLogFile);
                    if (fileInfo.Length >= MaxLogFileSize)
                    {
                        // Create a new log file if the current one is too large
                        InitializeCurrentLogFile();
                    }
                }
                else
                {
                    // If file doesn't exist (e.g., manually deleted), create a new one
                    InitializeCurrentLogFile();
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
                DateTime cutoffDate = DateTime.UtcNow.AddDays(value: -MaxLogFileAgeDays);
                string[] logFiles = Directory.GetFiles(path: _logDirectory, searchPattern: "log_*.txt");

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
                    lock (_lockObject)
                    {
                        if (file == _currentLogFile)
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
                System.Diagnostics.Debug.WriteLine(message: $"Error during log cleanup: {ex.Message}");
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~FileLoggerSingleton()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
                _cleanupTimer?.Dispose();
            _disposed = true;
        }
    }
}
