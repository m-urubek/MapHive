using MapHive.Models.Enums;
using MapHive.Models.RepositoryModels;
using MapHive.Singletons;
using System.Data;
using System.Data.SQLite;
using System.Text;

namespace MapHive.Repositories
{
    public class LogRepository : ILogRepository
    {
        private readonly ISqlClientSingleton _sqlClient;

        public LogRepository(ISqlClientSingleton sqlClient)
        {
            this._sqlClient = sqlClient;
        }

        public async Task<IEnumerable<LogGet>> GetLogsAsync(
            int page = 1,
            int pageSize = 20,
            string searchTerm = "",
            string sortField = "Timestamp",
            string sortDirection = "desc")
        {
            // Validate parameters
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);
            sortDirection = sortDirection.ToLowerInvariant() == "desc" ? "DESC" : "ASC";

            // Sanitize sort field to prevent SQL injection
            string sanitizedSortField = IsValidColumnName(sortField) ? QuoteIdentifier(sortField) : QuoteIdentifier("Timestamp");

            // Build query
            StringBuilder queryBuilder = new();
            List<SQLiteParameter> parameters = new();

            _ = queryBuilder.Append("SELECT l.*, s.Name AS SeverityName FROM Logs l ");
            _ = queryBuilder.Append("LEFT JOIN LogSeverity s ON l.SeverityId = s.Id_LogSeverity ");

            // Add WHERE clause
            _ = queryBuilder.Append("WHERE 1=1 ");

            // Add search term
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                _ = queryBuilder.Append("AND (l.Message LIKE @SearchTerm OR l.Source LIKE @SearchTerm OR l.UserName LIKE @SearchTerm OR l.RequestPath LIKE @SearchTerm) ");
                parameters.Add(new SQLiteParameter("@SearchTerm", $"%{searchTerm}%"));
            }

            // Add sorting
            _ = queryBuilder.Append($"ORDER BY {sanitizedSortField} {sortDirection} ");

            // Add pagination
            _ = queryBuilder.Append("LIMIT @PageSize OFFSET @Offset");
            parameters.Add(new SQLiteParameter("@PageSize", pageSize));
            parameters.Add(new SQLiteParameter("@Offset", (page - 1) * pageSize));

            // Execute query using injected _sqlClient
            DataTable result = await this._sqlClient.SelectAsync(queryBuilder.ToString(), parameters.ToArray());

            // Map results to LogGet objects
            List<LogGet> logs = new();
            foreach (DataRow row in result.Rows)
            {
                logs.Add(MapDataRowToLog(row));
            }

            return logs;
        }

        public async Task<int> GetTotalLogsCountAsync(string searchTerm = "")
        {
            // Build query
            StringBuilder queryBuilder = new();
            List<SQLiteParameter> parameters = new();

            _ = queryBuilder.Append("SELECT COUNT(*) FROM Logs l ");

            // Add WHERE clause
            _ = queryBuilder.Append("WHERE 1=1 ");

            // Add search term
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                _ = queryBuilder.Append("AND (l.Message LIKE @SearchTerm OR l.Source LIKE @SearchTerm OR l.UserName LIKE @SearchTerm OR l.RequestPath LIKE @SearchTerm) ");
                parameters.Add(new SQLiteParameter("@SearchTerm", $"%{searchTerm}%"));
            }

            // Execute query using injected _sqlClient
            DataTable result = await this._sqlClient.SelectAsync(queryBuilder.ToString(), parameters.ToArray());

            // Return count
            return result.Rows.Count > 0 && result.Rows[0][0] != DBNull.Value ? Convert.ToInt32(result.Rows[0][0]) : 0;
        }

        public async Task<LogGet?> GetLogByIdAsync(int id)
        {
            string query = @"
                SELECT l.*, s.Name AS SeverityName
                FROM Logs l
                LEFT JOIN LogSeverity s ON l.SeverityId = s.Id_LogSeverity
                WHERE l.Id_Log = @Id_Log";

            SQLiteParameter[] parameters = { new("@Id_Log", id) };

            // Use injected _sqlClient
            DataTable result = await this._sqlClient.SelectAsync(query, parameters);

            return result.Rows.Count == 0 ? null : MapDataRowToLog(result.Rows[0]);
        }

        public async Task<int> CreateLogRowAsync(LogCreate logCreate)
        {
            const string query = @"INSERT INTO Logs (Timestamp, SeverityId, Message, Source, Exception, UserId, RequestPath, AdditionalData)"
                                             + " VALUES (@Timestamp, @SeverityId, @Message, @Source, @Exception, @UserId, @RequestPath, @AdditionalData);";
            SQLiteParameter[] parameters = new SQLiteParameter[]
            {
                        new("@Timestamp", logCreate.Timestamp.ToString("o")),
                        new("@SeverityId", (int)logCreate.Severity),
                        new("@Message", logCreate.Message),
                        new("@Source", logCreate.Source as object ?? DBNull.Value),
                        new("@Exception", logCreate.Exception?.ToString() as object ?? DBNull.Value),
                        new("@UserId", logCreate.UserId as object ?? DBNull.Value),
                        new("@RequestPath", logCreate.RequestPath as object ?? DBNull.Value),
                        new("@AdditionalData", logCreate.AdditionalData as object ?? DBNull.Value)
            };
            return await this._sqlClient.InsertAsync(query, parameters);
        }

        private static LogGet MapDataRowToLog(DataRow row) //TODO make dynamic
        {
            // Simplified mapping
            return new LogGet
            {
                Id_Log = Convert.ToInt32(row["Id_Log"]),
                Timestamp = Convert.ToDateTime(row["Timestamp"]),
                SeverityId = Convert.ToInt32(row["SeverityId"]),
                Message = row["Message"].ToString() ?? string.Empty,
                Source = row["Source"]?.ToString(), // Nullable
                Exception = row["Exception"]?.ToString(), // Nullable
                UserId = row["UserId"] != DBNull.Value ? (int?)Convert.ToInt32(row["UserId"]) : null,
                UserName = row["UserName"]?.ToString(), // Nullable
                RequestPath = row["RequestPath"]?.ToString(), // Nullable
                AdditionalData = row["AdditionalData"]?.ToString(), // Nullable
                Severity = (LogSeverity)Convert.ToInt32(row["SeverityId"])
            };
        }

        // Keep this helper or replace with fetching from DB if severities change
        private static string GetSeverityName(int severityId)
        {
            return severityId switch
            {
                1 => "Information",
                2 => "Warning",
                3 => "Error",
                4 => "Critical",
                _ => "Unknown"
            };
        }

        // More robust column name validation/quoting
        private static readonly HashSet<string> _validSortColumns = new(StringComparer.OrdinalIgnoreCase)
        { "Id_Log", "Id_Log", "Timestamp", "SeverityId", "Message", "Source", "UserName", "RequestPath" };

        private static bool IsValidColumnName(string columnName)
        {
            return !string.IsNullOrWhiteSpace(columnName) && _validSortColumns.Contains(columnName);
        }

        // Helper to quote identifiers for SQLite
        private static string QuoteIdentifier(string identifier)
        {
            return $"\"{identifier.Replace("\"", "\"\"")}\""; // Basic quoting
        }
    }
}