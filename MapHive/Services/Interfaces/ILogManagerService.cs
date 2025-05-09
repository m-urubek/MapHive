using MapHive.Models.Enums;

namespace MapHive.Services
{
    public interface ILogManagerService
    {
        public void Log(
            LogSeverity severity,
            string message,
            Exception? exception = null,
            string? source = null,
            string? additionalData = null);
    }
}