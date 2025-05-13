namespace MapHive.Services
{
    using MapHive.Models.Enums;

    public interface ILogManagerService
    {
        public Task<int> LogAsync(
            LogSeverity severity,
            string message,
            Exception? exception = null,
            string? source = null,
            string? additionalData = null);
    }
}
