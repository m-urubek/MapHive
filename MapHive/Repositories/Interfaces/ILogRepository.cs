namespace MapHive.Repositories;

using MapHive.Models.Enums;

public interface ILogRepository
{
    /// <summary>
    /// Gets the total count of logs matching the search term
    /// </summary>
    /// <param name="searchTerm">Optional search term to filter logs</param>
    /// <returns>Total count of logs</returns>
    Task<int> GetTotalLogsCountAsync(
        string searchTerm = "");

    Task<int> CreateLogRowAsync(
        DateTime timestamp,
        LogSeverity severity,
        string message,
        string? source,
        Exception? exception,
        int? accountId,
        string? requestPath,
        string? additionalData);
}
