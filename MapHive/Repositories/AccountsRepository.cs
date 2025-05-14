namespace MapHive.Repositories
{
    using System.Data;
    using System.Data.SQLite;
    using System.Text.RegularExpressions;
    using MapHive.Models.Enums;
    using MapHive.Models.Exceptions;
    using MapHive.Models.RepositoryModels;
    using MapHive.Services;
    using MapHive.Singletons;
    using MapHive.Utilities.Extensions;

    public class AccountsRepository(ISqlClientSingleton sqlClientSingleton, ILogManagerService logManagerService) : IAccountsRepository
    {
        private readonly ISqlClientSingleton _sqlClientSingleton = sqlClientSingleton;
        private readonly ILogManagerService _logManagerService = logManagerService;

        // Create a new user using DTO
        public async Task<int> CreateAccountAsync(UserCreate createDto)
        {
            string query = @"
                INSERT into Accounts (Username, PasswordHash, RegistrationDate, Tier, IpAddressHistory)
                VALUES (@Username, @PasswordHash, @RegistrationDate, @Tier, @IpAddressHistory);";

            SQLiteParameter[] parameters =
            [
                new("@Username", createDto.Username),
                new("@PasswordHash", createDto.PasswordHash),
                new("@RegistrationDate", createDto.RegistrationDate.ToString(format: "yyyy-MM-dd HH:mm:ss")),
                new("@Tier", (int)createDto.Tier),
                new("@IpAddressHistory", createDto.IpAddressHistory)
            ];

            return await _sqlClientSingleton.InsertAsync(query: query, parameters: parameters);
        }

        // Basic user lookup
        public async Task<AccountGet?> GetAccountByIdAsync(int id)
        {
            string query = "SELECT * FROM Accounts WHERE Id_Account = @Id_Account";
            SQLiteParameter[] parameters = [new("@Id_Account", id)];
            DataTable result = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);
            return result.Rows.Count > 0 ? MapDataRowToaccountGet(row: result.Rows[0]) : null;
        }

        public async Task<AccountGet> GetAccountByIdOrThrowAsync(int id)
        {
            AccountGet? user = await GetAccountByIdAsync(id: id);
            return user ?? throw new PublicErrorException($"User \"{id}\" not found in database!");
        }

        public async Task<AccountGet?> GetAccountByUsernameAsync(string username)
        {
            string query = "SELECT * FROM Accounts WHERE Username = @Username";
            SQLiteParameter[] parameters = [new("@Username", username)];
            DataTable result = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);

            if (result.Rows.Count == 0)
            {
                throw new OrangeUserException($"Account \"{username}\" not found");
            }

            AccountGet user = MapDataRowToaccountGet(row: result.Rows[0]);

            return string.IsNullOrEmpty(value: user.PasswordHash) ? throw new Exception("User {username} found but has empty password hash") : user;
        }

        public async Task<AccountGet> GetAccountByUsernameOrThrowAsync(string username)
        {
            AccountGet? user = await GetAccountByUsernameAsync(username: username);
            return user ?? throw new PublicErrorException($"User \"{username}\" not found in database!");
        }

        public async Task<bool> CheckUsernameExistsAsync(string username)
        {
            string query = "SELECT 1 FROM Accounts WHERE Username = @Username LIMIT 1";
            SQLiteParameter[] parameters = [new("@Username", username)];
            DataTable result = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);
            return result.Rows.Count > 0;
        }

        public async Task<bool> IsIpBannedAsync(string hashedIpAddress)
        {
            string query = @"
                SELECT 1 FROM IpBans
                WHERE HashedIpAddress = @HashedIpAddress
                  AND (ExpiresAt IS NULL OR ExpiresAt > @Now)
                LIMIT 1";
            SQLiteParameter[] parameters =
            [
                new("@HashedIpAddress", hashedIpAddress),
                new("@Now", DateTime.UtcNow)
            ];
            DataTable result = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);
            return result.Rows.Count > 0;
        }

        public async Task<int> UpdateAccountAsync(UserUpdate updateDto)
        {
            string query = @"
                UPDATE Accounts
                SET Username = @Username,
                    PasswordHash = @PasswordHash,
                    Tier = @Tier,
                    IpAddressHistory = @IpAddressHistory
                WHERE Id_Account = @Id_Account";

            SQLiteParameter[] parameters =
            [
                new("@Id_Account", updateDto.Id),
                new("@Username", updateDto.Username),
                new("@PasswordHash", updateDto.PasswordHash as object ?? DBNull.Value),
                new("@Tier", (int)updateDto.Tier),
                new("@IpAddressHistory", updateDto.IpAddressHistory)
            ];
            return await _sqlClientSingleton.UpdateAsync(query: query, parameters: parameters);
        }

        public async Task<string> GetUsernameByIdAsync(int accountId)
        {
            string query = "SELECT Username FROM Accounts WHERE Id_Account = @Id_Account";
            SQLiteParameter[] parameters = [new("@Id_Account", accountId)];
            DataTable result = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);
            return result.Rows.Count > 0 && result.Rows[0]["Username"] != DBNull.Value
                 ? result.Rows[0]["Username"].ToString()!
                 : "Unknown"; // Provide a default value
        }

        public async Task<IEnumerable<AccountGet>> GeAccountsAsync(string searchTerm, int page, int pageSize, string sortColumnName = "", string sortDirection = "asc")
        {
            // Validate and sanitize sort field/direction
            HashSet<string> validSortFields = new()
            { "Username", "RegistrationDate", "Tier" };
            string orderBy = validSortFields.Contains(item: sortColumnName) ? sortColumnName : "Username";
            string direction = sortDirection.Equals("desc", StringComparison.CurrentCultureIgnoreCase) ? "DESC" : "ASC";

            string whereClause = string.IsNullOrWhiteSpace(value: searchTerm)
                ? ""
                : "WHERE Username LIKE @SearchTerm";

            string query = $@"
                SELECT * FROM Accounts
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
            List<AccountGet> list = new();
            foreach (DataRow row in result.Rows)
            {
                list.Add(item: MapDataRowToaccountGet(row: row));
            }

            return list;
        }

        public async Task<int> GetTotalAccountsCountAsync(string searchTerm)
        {
            string query = string.IsNullOrWhiteSpace(value: searchTerm)
                ? "SELECT COUNT(*) FROM Accounts"
                : "SELECT COUNT(*) FROM Accounts WHERE Username LIKE @SearchTerm";
            SQLiteParameter[] parameters = string.IsNullOrWhiteSpace(value: searchTerm)
                ? []
                : [new("@SearchTerm", $"%{searchTerm}%")];
            DataTable result = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);
            return Convert.ToInt32(value: result.Rows[0][0]);
        }

        public async Task<bool> UpdateAccountTierAsync(AccountTierUpdate tierDto)
        {
            string query = "UPDATE Accounts SET Tier = @Tier WHERE Id_Account = @AccountId";
            SQLiteParameter[] parameters = [new("@Tier", (int)tierDto.Tier), new("@AccountId", tierDto.AccountId)];
            int rows = await _sqlClientSingleton.UpdateAsync(query: query, parameters: parameters);
            return rows > 0;
        }

        private AccountGet MapDataRowToaccountGet(DataRow row)
        {
            return new AccountGet
            {
                Id = row.GetValueOrThrow<int>(columnName: "Id_Account"),
                Username = row.GetValueOrThrow<string>(columnName: "Username"),
                PasswordHash = row.GetValueOrThrow<string>(columnName: "PasswordHash"),
                Tier = (AccountTier)row.GetValueOrThrow<int>(columnName: "Tier"),
                RegistrationDate = row.GetValueOrThrow<DateTime>(columnName: "RegistrationDate"),
                IpAddressHistory = row.GetValueOrThrow<string>(columnName: "IpAddressHistory")
            };
        }
    }
}
