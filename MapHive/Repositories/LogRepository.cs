namespace MapHive.Repositories
{
    using System.Data;
    using System.Data.SQLite;
    using System.Text;
    using MapHive.Models.RepositoryModels;
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

        public async Task<int> CreateLogRowAsync(LogCreate logCreate)
        {
            const string query = "INSERT INTO Logs (Timestamp, SeverityId, Message, Source, Exception, AccountId, RequestPath, AdditionalData)"
                                             + " VALUES (@Timestamp, @SeverityId, @Message, @Source, @Exception, @AccountId, @RequestPath, @AdditionalData);";
            SQLiteParameter[] parameters =
            [
                        new("@Timestamp", logCreate.Timestamp.ToString(format: "o")),
                        new("@SeverityId", (int)logCreate.Severity),
                        new("@Message", logCreate.Message),
                        new("@Source", logCreate.Source as object ?? DBNull.Value),
                        new("@Exception", logCreate.Exception?.ToString() as object ?? DBNull.Value),
                        new("@AccountId", logCreate.AccountId as object ?? DBNull.Value),
                        new("@RequestPath", logCreate.RequestPath as object ?? DBNull.Value),
                        new("@AdditionalData", logCreate.AdditionalData as object ?? DBNull.Value)
            ];
            return await _sqlClientSingleton.InsertAsync(query: query, parameters: parameters);
        }
    }
}
