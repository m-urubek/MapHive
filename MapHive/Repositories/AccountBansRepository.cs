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
    using MapHive.Utilities.Extensions;

    public partial class AccountBansRepository(
        ISqlClientSingleton sqlClientSingleton,
        ILogManagerService logManagerService) : IAccountBansRepository
    {
        private readonly ISqlClientSingleton _sqlClientSingleton = sqlClientSingleton;
        private readonly ILogManagerService _logManagerService = logManagerService;

        public async Task<int> BanAccountAsync(AccountBanCreate accountBanCreate)
        {
            string query = @"
                INSERT INTO AccountBans (AccountId, BannedAt, ExpiresAt, Reason, BannedByAccountId)
                VALUES (@AccountId, @BannedAt, @ExpiresAt, @Reason, @BannedByAccountId);";
            SQLiteParameter[] parameters = [
                new("@AccountId", accountBanCreate.AccountId as object ?? DBNull.Value),
                new("@BannedAt", accountBanCreate.BannedAt.ToString("yyyy-MM-dd HH:mm:ss")),
                new("@ExpiresAt", accountBanCreate.ExpiresAt as object ?? DBNull.Value),
                new("@Reason", accountBanCreate.Reason),
                new("@BannedByAccountId", accountBanCreate.BannedByAccountId)
            ];
            return await _sqlClientSingleton.InsertAsync(query: query, parameters: parameters);
        }

        public async Task<bool> RemoveAccountBanAsync(int banId)
        {
            string query = "UPDATE AccountBans SET ExpiresAt = @ExpiresAt WHERE Id_AccountBan = @Id_AccountBan";
            SQLiteParameter[] parameters = [
                new("@ExpiresAt", DateTime.UtcNow),
                new("@Id_AccountBan", banId)
            ];
            int rows = await _sqlClientSingleton.UpdateAsync(query: query, parameters: parameters);
            return rows > 0;
        }

        public async Task<AccountBanGet?> GetActiveAccountBanByAccountIdAsync(int accountId)
        {
            string query = @"
                SELECT * FROM AccountBans
                WHERE AccountId = @AccountId AND (ExpiresAt IS NULL OR ExpiresAt > @Now)
                ORDER BY BannedAt DESC LIMIT 1";
            SQLiteParameter[] parameters = [new("@AccountId", accountId), new("@Now", DateTime.UtcNow)];
            DataTable result = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);
            return result.Rows.Count > 0 ? MapDataRowToAccountBanGet(row: result.Rows[0]) : null;
        }

        public async Task<AccountBanGet?> GetActiveAccountBanByUsernameAsync(string username)
        {
            string query = @" 
                SELECT AccountBans.* FROM AccountBans
                JOIN Users ON AccountBans.AccountId = Users.Id_Account
                WHERE Users.Username = @Username AND (AccountBans.ExpiresAt IS NULL OR AccountBans.ExpiresAt > @Now)
                ORDER BY AccountBans.BannedAt DESC LIMIT 1";
            SQLiteParameter[] parameters = [new("@Username", username), new("@Now", DateTime.UtcNow)];
            DataTable result = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);
            return result.Rows.Count > 0 ? MapDataRowToAccountBanGet(row: result.Rows[0]) : null;
        }

        public async Task<IEnumerable<AccountBanGet>> GetAllActiveBansAsync()
        {
            string query = "SELECT * FROM AccountBans WHERE ExpiresAt IS NULL OR ExpiresAt > @Now ORDER BY BannedAt DESC";
            SQLiteParameter[] parameters = [new("@Now", DateTime.UtcNow)];
            DataTable result = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);
            List<AccountBanGet> list = new();
            foreach (DataRow row in result.Rows)
            {
                list.Add(MapDataRowToAccountBanGet(row));
            }
            return list;
        }

        public async Task<IEnumerable<AccountBanGet>> GetAllBansAsync(string searchTerm = "", int page = 1, int pageSize = 20, string sortColumnName = "", string sortDirection = "asc")
        {
            HashSet<string> validSortFields = new() { "BannedAt", "ExpiresAt", "Reason" };
            string orderBy = validSortFields.Contains(sortColumnName) ? sortColumnName : "BannedAt";
            string direction = sortDirection.Equals("desc", StringComparison.CurrentCultureIgnoreCase) ? "DESC" : "ASC";

            string whereClause = string.IsNullOrWhiteSpace(searchTerm)
                ? string.Empty
                : "WHERE Reason LIKE @SearchTerm";

            string query = $@"
                SELECT * FROM AccountBans
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
            List<AccountBanGet> list = new();
            foreach (DataRow row in result.Rows)
            {
                list.Add(MapDataRowToAccountBanGet(row));
            }
            return list;
        }

        private AccountBanGet MapDataRowToAccountBanGet(DataRow row)
        {
            return new AccountBanGet
            {
                Id = row.GetValueOrThrow<int>(columnName: "Id_AccountBan"),
                AccountId = row.GetValueOrThrow<int>(columnName: "AccountId"),
                HashedIpAddress = row.GetAsNullableString(columnName: "HashedIpAddress"),
                BannedByAccountId = row.GetValueOrThrow<int>(columnName: "BannedByAccountId"),
                Reason = row.GetAsNullableString(columnName: "Reason"),
                BannedAt = row.GetValueOrThrow<DateTime>(columnName: "BannedAt"),
                ExpiresAt = row.GetValueNullable<DateTime>(columnName: "ExpiresAt"),
            };
        }
    }
}
