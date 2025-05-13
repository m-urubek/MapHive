namespace MapHive.Singletons
{
    /// <summary>
    /// Interface for logging messages to a file.
    /// </summary>
    public interface IFileLoggerSingleton
    {
        /// <summary>
        /// Writes a log entry to the configured log file.
        /// </summary>
        /// <param name="message">The message to log.</param>
        void LogToFile(string message);
    }
}