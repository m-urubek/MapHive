using Extensions;

namespace MapHive.Services
{
    public class FileLogger
    {
        private static readonly object _lockObject = new();
        private static string _currentLogFile;
        private static DateTime _currentLogFileCreationTime;
        private const long MaxLogFileSize = 1024 * 1024; // 1MB
        private const int MaxLogFileAgeDays = 30;
        private static readonly string _logDirectory;
        private static readonly Timer _cleanupTimer;

        static FileLogger()
        {
            _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

            // Ensure log directory exists
            if (!Directory.Exists(_logDirectory))
            {
                _ = Directory.CreateDirectory(_logDirectory);
            }

            // Initialize the current log file on startup
            InitializeCurrentLogFile();

            // Set up a timer to check for old log files once per day
            _cleanupTimer = new Timer(CleanupOldLogFiles, null, TimeSpan.Zero, TimeSpan.FromDays(1));
        }

        /// <summary>
        /// Writes a log entry to the current log file
        /// </summary>
        public static void LogToFile(string message)
        {
            // Ensure we have a valid log file that's not too large
            EnsureLogFile();

            // Format the log entry with timestamp
            string formattedMessage = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}";

            // Write to the file using the thread-safe writer
            ThreadSafeFileWriter.Write(_currentLogFile, formattedMessage);
        }

        /// <summary>
        /// Initializes the current log file
        /// </summary>
        private static void InitializeCurrentLogFile()
        {
            lock (_lockObject)
            {
                _currentLogFileCreationTime = DateTime.UtcNow;
                string timestamp = _currentLogFileCreationTime.ToString("yyyyMMdd_HHmmss");
                _currentLogFile = Path.Combine(_logDirectory, $"log_{timestamp}.txt");
            }
        }

        /// <summary>
        /// Ensures the current log file exists and is not too large
        /// </summary>
        private static void EnsureLogFile()
        {
            if (string.IsNullOrEmpty(_currentLogFile))
            {
                InitializeCurrentLogFile();
                return;
            }

            lock (_lockObject)
            {
                // Check if the current log file exists and is not too large
                if (File.Exists(_currentLogFile))
                {
                    FileInfo fileInfo = new(_currentLogFile);
                    if (fileInfo.Length >= MaxLogFileSize)
                    {
                        // Create a new log file if the current one is too large
                        InitializeCurrentLogFile();
                    }
                }
            }
        }

        /// <summary>
        /// Cleans up log files older than 30 days
        /// </summary>
        private static void CleanupOldLogFiles(object state)
        {
            try
            {
                DateTime cutoffDate = DateTime.UtcNow.AddDays(-MaxLogFileAgeDays);
                string[] logFiles = Directory.GetFiles(_logDirectory, "log_*.txt");

                foreach (string file in logFiles)
                {
                    // Skip the current log file
                    if (file == _currentLogFile)
                    {
                        continue;
                    }

                    FileInfo fileInfo = new(file);
                    if (fileInfo.CreationTimeUtc < cutoffDate)
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch (IOException)
                        {
                            // File might be in use, try again later
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Log cleanup failure to error log file
                // We don't want to use LogToFile here to avoid infinite recursion if there's an issue
            }
        }
    }
}