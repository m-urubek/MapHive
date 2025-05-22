namespace MapHive.Singletons;

using MapHive.Models.Enums;

public interface ILogManagerSingleton
{
    public Task<int> LogAsync(
        LogSeverity severity,
        string message,
        Exception? exception = null,
        string? source = null,
        string? additionalData = null,
        int? accountId = null,
        string? requestPath = null);
}
