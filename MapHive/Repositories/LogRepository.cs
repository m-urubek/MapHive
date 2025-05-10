namespace MapHive.Repositories
{
    using System.Data;
    using System.Data.SQLite;
    using System.Text;
    using MapHive.Models.Enums;
    using MapHive.Models.RepositoryModels;
    using MapHive.Services;
    using MapHive.Singletons;
    using MapHive.Utilities;

    public class LogRepository(ISqlClientSingleton sqlClientSingleton) : ILogRepository
    {
        private readonly ISqlClientSingleton _sqlClientSingleton = sqlClientSingleton;
        public async Task<IEnumerable<LogGet>> GetLogsAsync(
            int page = 1,
            int pageSize = 20,
            string searchTerm = "",
            string sortField = "Timestamp",
            string sortDirection = "desc")
        {
            // Validate parameters
            page = Math.Max(val1: 1, val2: page);
            pageSize = Math.Clamp(value: pageSize, min: 1, max: 100);
            sortDirection = sortDirection.Equals("desc", StringComparison.InvariantCultureIgnoreCase) ? "DESC" : "ASC";

            // Sanitize sort field to prevent SQL injection
            string sanitizedSortField = IsValidColumnName(columnName: sortField) ? QuoteIdentifier(identifier: sortField) : QuoteIdentifier(identifier: "Timestamp");

            // Build query
            StringBuilder queryBuilder = new();
            List<SQLiteParameter> parameters = new();

            _ = queryBuilder.Append(value: "SELECT l.*, s.Name AS SeverityName FROM Logs l ");
            _ = queryBuilder.Append(value: "LEFT JOIN LogSeverity s ON l.SeverityId = s.Id_LogSeverity ");

            // Add WHERE clause
            _ = queryBuilder.Append(value: "WHERE 1=1 ");

            // Add search term
            if (!string.IsNullOrWhiteSpace(value: searchTerm))
            {
                _ = queryBuilder.Append(value: "AND (l.Message LIKE @SearchTerm OR l.Source LIKE @SearchTerm OR l.UserName LIKE @SearchTerm OR l.RequestPath LIKE @SearchTerm) ");
                parameters.Add(item: new SQLiteParameter("@SearchTerm", $"%{searchTerm}%"));
            }

            // Add sorting
            _ = queryBuilder.Append(handler: $"ORDER BY {sanitizedSortField} {sortDirection} ");

            // Add pagination
            _ = queryBuilder.Append(value: "LIMIT @PageSize OFFSET @Offset");
            parameters.Add(item: new SQLiteParameter("@PageSize", pageSize));
            parameters.Add(item: new SQLiteParameter("@Offset", (page - 1) * pageSize));

            // Execute query using injected _sqlClientSingleton
            DataTable result = await _sqlClientSingleton.SelectAsync(query: queryBuilder.ToString(), parameters: parameters.ToArray());

            // Map results to LogGet objects
            List<LogGet> logs = new();
            foreach (DataRow row in result.Rows)
            {
                logs.Add(item: MapDataRowToLog(row: row));
            }

            return logs;
        }

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
            DataTable result = await _sqlClientSingleton.SelectAsync(query: queryBuilder.ToString(), parameters: parameters.ToArray());

            // Return count
            return result.Rows.Count > 0 && result.Rows[0][0] != DBNull.Value ? Convert.ToInt32(value: result.Rows[0][0]) : 0;
        }

        public async Task<LogGet?> GetLogByIdAsync(int id)
        {
            string query = @"
                SELECT l.*, s.Name AS SeverityName
                FROM Logs l
                LEFT JOIN LogSeverity s ON l.SeverityId = s.Id_LogSeverity
                WHERE l.Id_Log = @Id_Log";

            SQLiteParameter[] parameters = { new("@Id_Log", id) };

            // Use injected _sqlClientSingleton
            DataTable result = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);

            return result.Rows.Count == 0 ? null : MapDataRowToLog(row: result.Rows[0]);
        }

        public async Task<int> CreateLogRowAsync(LogCreate logCreate)
        {
            const string query = "INSERT INTO Logs (Timestamp, SeverityId, Message, Source, Exception, UserId, RequestPath, AdditionalData)"
                                             + " VALUES (@Timestamp, @SeverityId, @Message, @Source, @Exception, @UserId, @RequestPath, @AdditionalData);";
            SQLiteParameter[] parameters = new SQLiteParameter[]
            {
                        new("@Timestamp", logCreate.Timestamp.ToString(format: "o")),
                        new("@SeverityId", (int)logCreate.Severity),
                        new("@Message", logCreate.Message),
                        new("@Source", logCreate.Source as object ?? DBNull.Value),
                        new("@Exception", logCreate.Exception?.ToString() as object ?? DBNull.Value),
                        new("@UserId", logCreate.UserId as object ?? DBNull.Value),
                        new("@RequestPath", logCreate.RequestPath as object ?? DBNull.Value),
                        new("@AdditionalData", logCreate.AdditionalData as object ?? DBNull.Value)
            };
            return await _sqlClientSingleton.InsertAsync(query: query, parameters: parameters);
        }

        private LogGet MapDataRowToLog(DataRow row)
        {
            return new LogGet
            {
                Id_Log = row.Field<int?>("Id_Log") ?? throw new NoNullAllowedException("Id_Log"),
                Timestamp = row.Field<DateTime?>("Timestamp") ?? throw new NoNullAllowedException("Timestamp"),
                Message = row.Field<string?>("Message") ?? throw new NoNullAllowedException("Message"),
                Source = row.Field<string?>("Source"),
                Exception = row.Field<string?>("Exception"),
                UserId = row.Field<int?>("UserId"),
                RequestPath = row.Field<string?>("RequestPath"),
                AdditionalData = row.Field<string?>("AdditionalData"),
                Severity = row.Field<LogSeverity?>("SeverityId") ?? throw new NoNullAllowedException("SeverityId")
            };
        }

        // More robust column name validation/quoting
        private static readonly HashSet<string> _validSortColumns = new(StringComparer.OrdinalIgnoreCase)
        { "Id_Log", "Id_Log", "Timestamp", "SeverityId", "Message", "Source", "UserName", "RequestPath" };

        private static bool IsValidColumnName(string columnName)
        {
            return !string.IsNullOrWhiteSpace(value: columnName) && _validSortColumns.Contains(item: columnName);
        }

        // Helper to quote identifiers for SQLite
        private static string QuoteIdentifier(string identifier)
        {
            return $"\"{identifier.Replace(oldValue: "\"", newValue: "\"\"")}\""; // Basic quoting
        }
    }
}
