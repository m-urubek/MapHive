namespace MapHive.Singletons
{
    public interface ILogManagerSingleton
    {
        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="source">The source of the log (optional).</param>
        /// <param name="exception">An optional exception.</param>
        /// <param name="additionalData">Any additional data.</param>
        void Information(string message, string? source = null, Exception? exception = null, string? additionalData = null);

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="source">The source of the log (optional).</param>
        /// <param name="exception">An optional exception.</param>
        /// <param name="additionalData">Any additional data.</param>
        void Warning(string message, string? source = null, Exception? exception = null, string? additionalData = null);

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="source">The source of the log (optional).</param>
        /// <param name="exception">An optional exception.</param>
        /// <param name="additionalData">Any additional data.</param>
        void Error(string message, string? source = null, Exception? exception = null, string? additionalData = null);

        /// <summary>
        /// Logs a critical error message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="source">The source of the log (optional).</param>
        /// <param name="exception">An optional exception.</param>
        /// <param name="additionalData">Any additional data.</param>
        void Critical(string message, string? source = null, Exception? exception = null, string? additionalData = null);
    }
}