namespace MapHive.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SQLite;
    using System.Threading.Tasks;
    using MapHive.Models.Enums;
    using MapHive.Models.RepositoryModels;
    using MapHive.Services;
    using MapHive.Singletons;
    using MapHive.Utilities;

    public partial class UserBansRepository(
        ISqlClientSingleton sqlClientSingleton,
        ILogManagerService logManagerService) : IUserBansRepository
    {
        private readonly ISqlClientSingleton _sqlClientSingleton = sqlClientSingleton;
        private readonly ILogManagerService _logManagerService = logManagerService;

        public async Task<int> BanUserAsync(UserBanGetCreate banDto)
        {
            string query = @"
                INSERT INTO UserBans (UserId, HashedIpAddress, BanType, BannedAt, ExpiresAt, Reason, BannedByUserId)
                VALUES (@UserId, @HashedIpAddress, @BanType, @BannedAt, @ExpiresAt, @Reason, @BannedByUserId);";
            SQLiteParameter[] parameters = [
                new("@UserId", banDto.UserId as object ?? DBNull.Value),
                new("@HashedIpAddress", banDto.HashedIpAddress as object ?? DBNull.Value),
                new("@BanType", (int)banDto.BanType),
                new("@BannedAt", banDto.BannedAt.ToString("yyyy-MM-dd HH:mm:ss")),
                new("@ExpiresAt", banDto.ExpiresAt as object ?? DBNull.Value),
                new("@Reason", banDto.Reason),
                new("@BannedByUserId", banDto.BannedByUserId)
            ];
            return await _sqlClientSingleton.InsertAsync(query: query, parameters: parameters);
        }

        public async Task<bool> RemoveUserBanAsync(int banId)
        {
            string query = "UPDATE UserBans SET ExpiresAt = @ExpiresAt WHERE Id_UserBan = @Id_UserBan";
            SQLiteParameter[] parameters = [
                new("@ExpiresAt", DateTime.UtcNow),
                new("@Id_UserBan", banId)
            ];
            int rows = await _sqlClientSingleton.UpdateAsync(query: query, parameters: parameters);
            return rows > 0;
        }

        public async Task<UserBanGet?> GetActiveBanByUserIdAsync(int userId)
        {
            string query = @"
                SELECT * FROM UserBans
                WHERE UserId = @UserId AND (ExpiresAt IS NULL OR ExpiresAt > @Now)
                ORDER BY BannedAt DESC LIMIT 1";
            SQLiteParameter[] parameters = [new("@UserId", userId), new("@Now", DateTime.UtcNow)];
            DataTable result = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);
            return result.Rows.Count > 0 ? MapDataRowToUserBanGet(row: result.Rows[0]) : null;
        }

        public async Task<UserBanGet?> GetActiveBanByIpAddressAsync(string hashedIpAddress)
        {
            string query = @"
                SELECT * FROM UserBans
                WHERE HashedIpAddress = @HashedIpAddress AND (ExpiresAt IS NULL OR ExpiresAt > @Now)
                ORDER BY BannedAt DESC LIMIT 1";
            SQLiteParameter[] parameters = [new("@HashedIpAddress", hashedIpAddress), new("@Now", DateTime.UtcNow)];
            DataTable result = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);
            return result.Rows.Count > 0 ? MapDataRowToUserBanGet(row: result.Rows[0]) : null;
        }

        public async Task<IEnumerable<UserBanGet>> GetAllActiveBansAsync()
        {
            string query = "SELECT * FROM UserBans WHERE ExpiresAt IS NULL OR ExpiresAt > @Now ORDER BY BannedAt DESC";
            SQLiteParameter[] parameters = [new("@Now", DateTime.UtcNow)];
            DataTable result = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);
            List<UserBanGet> list = new();
            foreach (DataRow row in result.Rows)
            {
                list.Add(MapDataRowToUserBanGet(row));
            }
            return list;
        }

        public async Task<IEnumerable<UserBanGet>> GetAllBansAsync(string searchTerm = "", int page = 1, int pageSize = 20, string sortColumnName = "", string sortDirection = "asc")
        {
            HashSet<string> validSortFields = new() { "BannedAt", "ExpiresAt", "Reason" };
            string orderBy = validSortFields.Contains(sortColumnName) ? sortColumnName : "BannedAt";
            string direction = sortDirection.Equals("desc", StringComparison.CurrentCultureIgnoreCase) ? "DESC" : "ASC";

            string whereClause = string.IsNullOrWhiteSpace(searchTerm)
                ? string.Empty
                : "WHERE Reason LIKE @SearchTerm";

            string query = $@"
                SELECT * FROM UserBans
                {whereClause}
                ORDER BY {orderBy} {direction}
                LIMIT @PageSize OFFSET @Offset";

            List<SQLiteParameter> parametersList = new();
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                parametersList.Add(new SQLiteParameter("@SearchTerm", $"%{searchTerm}%"));
            }
            parametersList.Add(new SQLiteParameter("@PageSize", pageSize));
            parametersList.Add(new SQLiteParameter("@Offset", (page - 1) * pageSize));

            DataTable result = await _sqlClientSingleton.SelectAsync(query: query, parameters: [.. parametersList]);
            List<UserBanGet> list = new();
            foreach (DataRow row in result.Rows)
            {
                list.Add(MapDataRowToUserBanGet(row));
            }
            return list;
        }

        private UserBanGet MapDataRowToUserBanGet(DataRow row)
        {
            const string table = "UserBans";
            return new UserBanGet
            {
                Id = row.GetValueOrDefault(_logManagerService, tableName: table, columnName: "Id_UserBan", isRequired: true, converter: Convert.ToInt32),
                UserId = row.Table.Columns.Contains("UserId") && row["UserId"] != DBNull.Value ? Convert.ToInt32(row["UserId"]) : null,
                HashedIpAddress = row.Table.Columns.Contains("HashedIpAddress") && row["HashedIpAddress"] != DBNull.Value ? row["HashedIpAddress"].ToString()! : null,
                BannedByUserId = row.GetValueOrDefault(_logManagerService, tableName: table, columnName: "BannedByUserId", isRequired: true, converter: Convert.ToInt32),
                Reason = row.GetValueOrDefault(_logManagerService, tableName: table, columnName: "Reason", isRequired: true, converter: v => v.ToString()!, defaultValue: string.Empty),
                BanType = row.GetValueOrDefault(_logManagerService, tableName: table, columnName: "BanType", isRequired: true, converter: v => (BanType)Convert.ToInt32(v)),
                BannedAt = row.GetValueOrDefault(_logManagerService, tableName: table, columnName: "BannedAt", isRequired: true, converter: Convert.ToDateTime),
                ExpiresAt = row.Table.Columns.Contains("ExpiresAt") && row["ExpiresAt"] != DBNull.Value && DateTime.TryParse(row["ExpiresAt"].ToString(), out DateTime parsedDateTime) ? parsedDateTime : null,
                IsActive = row.Table.Columns.Contains("ExpiresAt") && (row["ExpiresAt"] == DBNull.Value || (DateTime.TryParse(row["ExpiresAt"].ToString(), out DateTime parsed2) && parsed2 > DateTime.UtcNow)),
                Properties = new Dictionary<string, string>()
            };
        }
    }
}
