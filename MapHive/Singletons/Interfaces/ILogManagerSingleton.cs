namespace MapHive.Singletons
{
    using MapHive.Models.Enums;

    public interface ILogManagerSingleton
    {
        public void Log(
            LogSeverity severity,
            string message,
            Exception? exception = null,
            string? source = null,
            string? additionalData = null,
            int? userId = null,
            string? requestPath = null);
    }
}