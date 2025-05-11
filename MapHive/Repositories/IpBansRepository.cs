namespace MapHive.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SQLite;
    using System.Text.RegularExpressions;
    using MapHive.Models.RepositoryModels;
    using MapHive.Services;
    using MapHive.Singletons;
    using MapHive.Utilities;

    public partial class IpBansRepository(
        ISqlClientSingleton sqlClientSingleton,
        ILogManagerService logManagerService) : IIpBansRepository
    {
        private readonly ISqlClientSingleton _sqlClientSingleton = sqlClientSingleton;
        private readonly ILogManagerService _logManagerService = logManagerService;

        public async Task<bool> IsIpBannedAsync(string hashedIpAddress)
        {
            string query = @"
                SELECT 1 FROM IpBans
                WHERE HashedIpAddress = @HashedIpAddress
                  AND (ExpiresAt IS NULL OR ExpiresAt > @Now)
                LIMIT 1";
            SQLiteParameter[] parameters = [
                new("@HashedIpAddress", hashedIpAddress),
                new("@Now", DateTime.UtcNow)
            ];
            DataTable result = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);
            return result.Rows.Count > 0;
        }

        public async Task<int> CreateIpBanAsync(IpBanCreate ipBan)
        {
            if (string.IsNullOrEmpty(value: ipBan.IpAddress) || !MyRegex().IsMatch(input: ipBan.IpAddress))
            {
                throw new ArgumentException("IP address must be a valid SHA256 hash.");
            }

            string query = @"
                INSERT INTO IpBans (HashedIpAddress, Reason, BannedAt, ExpiresAt, BannedByUserId)
                VALUES (@HashedIpAddress, @Reason, @BannedAt, @ExpiresAt, @BannedByUserId);";
            SQLiteParameter[] parameters = [
                new("@HashedIpAddress", ipBan.IpAddress),
                new("@Reason", ipBan.Reason),
                new("@BannedAt", ipBan.BannedAt.ToString(format: "yyyy-MM-dd HH:mm:ss")),
                new("@ExpiresAt", ipBan.ExpiresAt.HasValue ? ipBan.ExpiresAt.Value.ToString(format: "yyyy-MM-dd HH:mm:ss") : DBNull.Value),
                new("@BannedByUserId", ipBan.BannedByUserId)
            ];
            return await _sqlClientSingleton.InsertAsync(query: query, parameters: parameters);
        }

        public async Task<bool> RemoveIpBanAsync(int banId)
        {
            string query = "UPDATE IpBans SET ExpiresAt = @ExpiresAt WHERE Id_IpBan = @Id_IpBan";
            SQLiteParameter[] parameters = [
                new("@ExpiresAt", DateTime.UtcNow),
                new("@Id_IpBan", banId)
            ];
            int rows = await _sqlClientSingleton.UpdateAsync(query: query, parameters: parameters);
            return rows > 0;
        }

        public async Task<IpBanGet?> GetActiveIpBanByIpAddressAsync(string hashedIpAddress)
        {
            string query = @"
                SELECT * FROM IpBans
                WHERE HashedIpAddress = @HashedIpAddress AND (ExpiresAt IS NULL OR ExpiresAt > @Now)
                ORDER BY BannedAt DESC LIMIT 1";
            SQLiteParameter[] parameters = [
                new("@HashedIpAddress", hashedIpAddress),
                new("@Now", DateTime.UtcNow)
            ];
            DataTable result = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);
            return result.Rows.Count > 0 ? MapDataRowToIpBanGet(row: result.Rows[0]) : null;
        }

        public async Task<IEnumerable<IpBanGet>> GetAllActiveIpBansAsync()
        {
            string query = "SELECT * FROM IpBans WHERE ExpiresAt IS NULL OR ExpiresAt > @Now ORDER BY BannedAt DESC";
            SQLiteParameter[] parameters = [new("@Now", DateTime.UtcNow)];
            DataTable result = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);
            List<IpBanGet> list = new();
            foreach (DataRow row in result.Rows)
            {
                list.Add(item: MapDataRowToIpBanGet(row: row));
            }
            return list;
        }

        public async Task<IEnumerable<IpBanGet>> GetAllIpBansAsync(string searchTerm = "", int page = 1, int pageSize = 20, string sortColumnName = "", string sortDirection = "asc")
        {
            // Validate and sanitize sort field/direction
            HashSet<string> validSortFields = new() { "BannedAt", "ExpiresAt", "Reason" };
            string orderBy = validSortFields.Contains(item: sortColumnName) ? sortColumnName : "BannedAt";
            string direction = sortDirection.Equals("desc", StringComparison.CurrentCultureIgnoreCase) ? "DESC" : "ASC";

            string whereClause = string.IsNullOrWhiteSpace(value: searchTerm)
                ? ""
                : "WHERE Reason LIKE @SearchTerm";

            string query = $@"
                SELECT * FROM IpBans
                {whereClause}
                ORDER BY {orderBy} {direction}
                LIMIT @PageSize OFFSET @Offset";

            List<SQLiteParameter> parametersList = new();
            if (!string.IsNullOrWhiteSpace(value: searchTerm))
            {
                parametersList.Add(item: new SQLiteParameter("@SearchTerm", $"%{searchTerm}%"));
            }
            parametersList.Add(item: new SQLiteParameter("@PageSize", pageSize));
            parametersList.Add(item: new SQLiteParameter("@Offset", (page - 1) * pageSize));

            DataTable result = await _sqlClientSingleton.SelectAsync(query: query, parameters: [.. parametersList]);
            List<IpBanGet> list = new();
            foreach (DataRow row in result.Rows)
            {
                list.Add(item: MapDataRowToIpBanGet(row: row));
            }
            return list;
        }

        private IpBanGet MapDataRowToIpBanGet(DataRow row)
        {
            const string table = "IpBans";
            return new IpBanGet
            {
                Id = row.GetValueOrDefault(_logManagerService, tableName: table, columnName: "Id_IpBan", isRequired: true, converter: Convert.ToInt32),
                HashedIpAddress = row.GetValueOrDefault(_logManagerService, tableName: table, columnName: "HashedIpAddress", isRequired: true, converter: v => v.ToString()!, defaultValue: string.Empty),
                Reason = row.GetValueOrDefault(_logManagerService, tableName: table, columnName: "Reason", isRequired: true, converter: v => v.ToString()!, defaultValue: string.Empty),
                BannedAt = row.GetValueOrDefault(_logManagerService, tableName: table, columnName: "BannedAt", isRequired: true, converter: Convert.ToDateTime),
                ExpiresAt = row.Table.Columns.Contains("ExpiresAt") && row["ExpiresAt"] != DBNull.Value && DateTime.TryParse(row["ExpiresAt"].ToString(), out DateTime parsed) ? parsed : null,
                BannedByUserId = row.GetValueOrDefault(_logManagerService, tableName: table, columnName: "BannedByUserId", isRequired: true, converter: Convert.ToInt32),
                IsActive = row.Table.Columns.Contains("ExpiresAt") && (row["ExpiresAt"] == DBNull.Value || (DateTime.TryParse(row["ExpiresAt"].ToString(), out DateTime parsed2) && parsed2 > DateTime.UtcNow)),
                Properties = new Dictionary<string, string>()
            };
        }

        [GeneratedRegex("^[a-fA-F0-9]{64}$")]
        private static partial Regex MyRegex();
    }
}
