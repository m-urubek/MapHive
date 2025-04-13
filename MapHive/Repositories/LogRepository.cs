using MapHive.Models;
using MapHive.Repositories.Interfaces;
using MapHive.Singletons;
using System.Data;
using System.Data.SQLite;
using System.Text;

namespace MapHive.Repositories
{
    public class LogRepository : ILogRepository
    {
        public async Task<IEnumerable<Log>> GetLogsAsync(
            int page = 1,
            int pageSize = 20,
            string searchTerm = "",
            string sortField = "Timestamp",
            string sortDirection = "desc")
        {
            // Validate parameters
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);
            sortDirection = sortDirection.ToLower() == "desc" ? "DESC" : "ASC";

            // Sanitize sort field to prevent SQL injection
            if (!IsValidColumnName(sortField))
            {
                sortField = "Timestamp";
            }

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
            _ = sortField == "Id"
                ? queryBuilder.Append($"ORDER BY l.Id_Log {sortDirection} ")
                : queryBuilder.Append($"ORDER BY l.{sortField} {sortDirection} ");

            // Add pagination
            _ = queryBuilder.Append("LIMIT @PageSize OFFSET @Offset");
            parameters.Add(new SQLiteParameter("@PageSize", pageSize));
            parameters.Add(new SQLiteParameter("@Offset", (page - 1) * pageSize));

            // Execute query
            DataTable result = await CurrentRequest.SqlClient.SelectAsync(queryBuilder.ToString(), parameters.ToArray());

            // Map results to Log objects
            List<Log> logs = new();
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

            // Execute query
            DataTable result = await CurrentRequest.SqlClient.SelectAsync(queryBuilder.ToString(), parameters.ToArray());

            // Return count
            return Convert.ToInt32(result.Rows[0][0]);
        }

        public async Task<Log?> GetLogByIdAsync(int id)
        {
            string query = @"
                SELECT l.*, s.Name AS SeverityName 
                FROM Logs l 
                LEFT JOIN LogSeverity s ON l.SeverityId = s.Id_LogSeverity 
                WHERE l.Id_Log = @Id";

            SQLiteParameter[] parameters = { new("@Id", id) };

            DataTable result = await CurrentRequest.SqlClient.SelectAsync(query, parameters);

            return result.Rows.Count == 0 ? null : MapDataRowToLog(result.Rows[0]);
        }

        public async Task<IEnumerable<LogSeverity>> GetLogSeveritiesAsync()
        {
            string query = "SELECT Id_LogSeverity as Id, Name, Description FROM LogSeverity ORDER BY Id_LogSeverity";

            DataTable result = await CurrentRequest.SqlClient.SelectAsync(query);

            List<LogSeverity> severities = new();
            foreach (DataRow row in result.Rows)
            {
                severities.Add(new LogSeverity
                {
                    Id = Convert.ToInt32(row["Id"]),
                    Name = row["Name"].ToString() ?? string.Empty,
                    Description = row["Description"].ToString() ?? string.Empty
                });
            }

            return severities;
        }

        private static Log MapDataRowToLog(DataRow row)
        {
            Log log = new()
            {
                Id = Convert.ToInt32(row["Id_Log"]),
                Timestamp = Convert.ToDateTime(row["Timestamp"]),
                SeverityId = Convert.ToInt32(row["SeverityId"]),
                Message = row["Message"].ToString() ?? string.Empty,
                Source = row["Source"].ToString() ?? string.Empty,
                Exception = row["Exception"].ToString() ?? string.Empty,
                UserName = row["UserName"].ToString() ?? string.Empty,
                RequestPath = row["RequestPath"].ToString() ?? string.Empty,
                AdditionalData = row["AdditionalData"].ToString() ?? string.Empty,
                // Create a basic LogSeverity object from the joined data
                Severity = new LogSeverity
                {
                    Id = Convert.ToInt32(row["SeverityId"]),
                    Name = row.Table.Columns.Contains("SeverityName") ?
                           (row["SeverityName"]?.ToString() ?? string.Empty) :
                           GetSeverityName(Convert.ToInt32(row["SeverityId"])),
                    Description = string.Empty // We don't have this in the query result
                }
            };

            return log;
        }

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

        private static bool IsValidColumnName(string columnName)
        {
            // List of valid column names
            string[] validColumns = { "Id", "Id_Log", "Timestamp", "SeverityId", "Message", "Source", "UserName", "RequestPath" };
            return Array.Exists(validColumns, c => c.Equals(columnName, StringComparison.OrdinalIgnoreCase));
        }
    }
}