namespace MapHive.Repositories;

using System.Data;
using System.Data.SQLite;
using System.Text;
using MapHive.Models.Enums;
using MapHive.Singletons;

public class LogRepository(ISqlClientSingleton sqlClientSingleton) : ILogRepository
{
    private readonly ISqlClientSingleton _sqlClientSingleton = sqlClientSingleton;

    public async Task<int> GetTotalLogsCountAsync(string searchTerm = "")
    {
        // Build query
        StringBuilder queryBuilder = new();
        List<SQLiteParameter> parameters = new();

        _ = queryBuilder.Append(value: "SELECT COUNT(*) FROM Logs l ");

        // Add WHERE clause
        _ = queryBuilder.Append(value: "WHERE 1=1 ");

        // Add search term
        if (!string.IsNullOrWhiteSpace(value: searchTerm))
        {
            _ = queryBuilder.Append(value: "AND (l.Message LIKE @SearchTerm OR l.Source LIKE @SearchTerm OR l.UserName LIKE @SearchTerm OR l.RequestPath LIKE @SearchTerm) ");
            parameters.Add(item: new SQLiteParameter("@SearchTerm", $"%{searchTerm}%"));
        }

        // Execute query using injected _sqlClientSingleton
        DataTable result = await _sqlClientSingleton.SelectAsync(query: queryBuilder.ToString(), parameters: [.. parameters]);

        // Return count
        return result.Rows.Count > 0 && result.Rows[0][0] != DBNull.Value ? Convert.ToInt32(value: result.Rows[0][0]) : 0;
    }

    public async Task<int> CreateLogRowAsync(
        DateTime timestamp,
        LogSeverity severity,
        string message,
        string? source,
        Exception? exception,
        int? accountId,
        string? requestPath,
        string? additionalData)
    {
        const string query = "INSERT INTO Logs (Timestamp, SeverityId, Message, Source, Exception, AuthorId, RequestPath, AdditionalData)"
                                         + " VALUES (@Timestamp, @SeverityId, @Message, @Source, @Exception, @AuthorId, @RequestPath, @AdditionalData);";
        SQLiteParameter[] parameters =
        [
            new("@Timestamp", timestamp.ToString(format: "o")),
            new("@SeverityId", (int)severity),
            new("@Message", message),
            new("@Source", source ?? null),
            new("@Exception", exception?.ToString() ?? null),
            new("@AuthorId", accountId ?? null),
            new("@RequestPath", requestPath ?? null),
            new("@AdditionalData", additionalData ?? null)
        ];
        return await _sqlClientSingleton.InsertAsync(query: query, parameters: parameters);
    }
}
