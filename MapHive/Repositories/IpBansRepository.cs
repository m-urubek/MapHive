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
    using MapHive.Utilities.Extensions;

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
            if (string.IsNullOrEmpty(value: ipBan.HashedIpAddress) || !MyRegex().IsMatch(input: ipBan.HashedIpAddress))
            {
                throw new ArgumentException("IP address must be a valid SHA256 hash.");
            }

            string query = @"
                INSERT INTO IpBans (HashedIpAddress, Reason, BannedAt, ExpiresAt, BannedByAccountId)
                VALUES (@HashedIpAddress, @Reason, @BannedAt, @ExpiresAt, @BannedByAccountId);";
            SQLiteParameter[] parameters = [
                new("@HashedIpAddress", ipBan.HashedIpAddress),
                new("@Reason", ipBan.Reason),
                new("@BannedAt", ipBan.BannedAt.ToString(format: "yyyy-MM-dd HH:mm:ss")),
                new("@ExpiresAt", ipBan.ExpiresAt.HasValue ? ipBan.ExpiresAt.Value.ToString(format: "yyyy-MM-dd HH:mm:ss") : DBNull.Value),
                new("@BannedByAccountId", ipBan.BannedByAccountId)
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
            return new IpBanGet
            {
                Id = row.GetValueOrThrow<int>(columnName: "Id_IpBan"),
                HashedIpAddress = row.GetValueOrThrow<string>(columnName: "HashedIpAddress"),
                Reason = row.GetAsNullableString(columnName: "Reason"),
                BannedAt = row.GetValueOrThrow<DateTime>(columnName: "BannedAt"),
                BannedByAccountId = row.GetValueOrThrow<int>(columnName: "BannedByAccountId"),
            };
        }

        [GeneratedRegex("^[a-fA-F0-9]{64}$")]
        private static partial Regex MyRegex();
    }
}
