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
    using MapHive.Utilities;

    public partial class UserRepository(ISqlClientSingleton sqlClientSingleton, ILogManagerService logManagerService) : IUserRepository
    {
        private readonly ISqlClientSingleton _sqlClientSingleton = sqlClientSingleton;
        private readonly ILogManagerService _logManagerService = logManagerService;

        // Create a new user using DTO
        public async Task<int> CreateUserAsync(UserCreate createDto)
        {
            string query = @"
                INSERT INTO Users (Username, PasswordHash, RegistrationDate, Tier, IpAddressHistory)
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
        public async Task<UserGet?> GetUserByIdAsync(int id)
        {
            string query = "SELECT * FROM Users WHERE Id_User = @Id_User";
            SQLiteParameter[] parameters = [new("@Id_User", id)];
            DataTable result = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);
            return result.Rows.Count > 0 ? MapDataRowToUserGet(row: result.Rows[0]) : null;
        }

        public async Task<UserGet> GetUserByIdOrThrowAsync(int id)
        {
            UserGet? user = await GetUserByIdAsync(id: id);
            return user ?? throw new PublicErrorException($"User \"{id}\" not found in database!");
        }

        public async Task<UserGet?> GetUserByUsernameAsync(string username)
        {
            string query = "SELECT * FROM Users WHERE Username = @Username";
            SQLiteParameter[] parameters = [new("@Username", username)];
            DataTable result = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);

            if (result.Rows.Count == 0)
            {
                throw new OrangeUserException($"User \"{username}\" not found");
            }

            UserGet user = MapDataRowToUserGet(row: result.Rows[0]);

            return string.IsNullOrEmpty(value: user.PasswordHash) ? throw new Exception("User {username} found but has empty password hash") : user;
        }

        public async Task<UserGet> GetUserByUsernameOrThrowAsync(string username)
        {
            UserGet? user = await GetUserByUsernameAsync(username: username);
            return user ?? throw new PublicErrorException($"User \"{username}\" not found in database!");
        }

        public async Task<bool> CheckUsernameExistsAsync(string username)
        {
            string query = "SELECT 1 FROM Users WHERE Username = @Username LIMIT 1";
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

        public async Task<int> CreateIpBanAsync(IpBanCreate ipBan)
        {
            // Basic validation for SHA256 hash format
            if (string.IsNullOrEmpty(value: ipBan.IpAddress) || !MyRegex().IsMatch(input: ipBan.IpAddress))
            {
                throw new Exception($"Attempted to ban an invalid or non-hashed IP format: {ipBan.IpAddress}");
                throw new ArgumentException("IP address must be a valid SHA256 hash.");
            }

            string query = @"
                INSERT INTO IpBans (HashedIpAddress, Reason, BannedAt, ExpiresAt, BannedByUserId)
                VALUES (@HashedIpAddress, @Reason, @BannedAt, @ExpiresAt, @BannedByUserId);";
            SQLiteParameter[] parameters =
            [
                new("@HashedIpAddress", ipBan.IpAddress),
                new("@Reason", ipBan.Reason),
                new("@BannedAt", ipBan.BannedAt.ToString(format: "yyyy-MM-dd HH:mm:ss")),
                new("@ExpiresAt", ipBan.ExpiresAt.HasValue ? ipBan.ExpiresAt.Value.ToString(format: "yyyy-MM-dd HH:mm:ss") : DBNull.Value),
                new("@BannedByUserId", ipBan.BannedByUserId)
            ];
            return await _sqlClientSingleton.InsertAsync(query: query, parameters: parameters);
        }

        public async Task<int> UpdateUserAsync(UserUpdate updateDto)
        {
            string query = @"
                UPDATE Users
                SET Username = @Username,
                    PasswordHash = @PasswordHash,
                    Tier = @Tier,
                    IpAddressHistory = @IpAddressHistory
                WHERE Id_User = @Id_User";

            SQLiteParameter[] parameters =
            [
                new("@Id_User", updateDto.Id),
                new("@Username", updateDto.Username),
                new("@PasswordHash", updateDto.PasswordHash as object ?? DBNull.Value),
                new("@Tier", (int)updateDto.Tier),
                new("@IpAddressHistory", updateDto.IpAddressHistory)
            ];
            return await _sqlClientSingleton.UpdateAsync(query: query, parameters: parameters);
        }

        public async Task<string> GetUsernameByIdAsync(int userId)
        {
            string query = "SELECT Username FROM Users WHERE Id_User = @Id_User";
            SQLiteParameter[] parameters = [new("@Id_User", userId)];
            DataTable result = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);
            return result.Rows.Count > 0 && result.Rows[0]["Username"] != DBNull.Value
                 ? result.Rows[0]["Username"].ToString()!
                 : "Unknown"; // Provide a default value
        }

        // Ban related methods
        public async Task<int> BanUserAsync(UserBanGetCreate banDto)
        {
            string query = @"
                INSERT INTO UserBans (UserId, HashedIpAddress, BanType, BannedAt, ExpiresAt, Reason, BannedByUserId)
                VALUES (@UserId, @HashedIpAddress, @BanType, @BannedAt, @ExpiresAt, @Reason, @BannedByUserId);";
            SQLiteParameter[] parameters =
            [
                new("@UserId", banDto.UserId as object ?? DBNull.Value),
                new("@HashedIpAddress", banDto.HashedIpAddress as object ?? DBNull.Value),
                new("@BanType", (int)banDto.BanType),
                new("@BannedAt", banDto.BannedAt),
                new("@ExpiresAt", banDto.ExpiresAt as object ?? DBNull.Value),
                new("@Reason", banDto.Reason),
                new("@BannedByUserId", banDto.BannedByUserId)
            ];
            return await _sqlClientSingleton.InsertAsync(query: query, parameters: parameters);
        }

        public async Task<bool> UnbanUserAsync(int banId)
        {
            string query = "UPDATE UserBans SET ExpiresAt = @ExpiresAt WHERE Id_UserBan = @Id_UserBan";
            SQLiteParameter[] parameters =
            [
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
            return result.Rows.Count > 0 ? MapDataRowToUserBanGetGet(row: result.Rows[0]) : null;
        }

        public async Task<UserBanGet?> GetActiveBanByIpAddressAsync(string hashedIpAddress)
        {
            string query = @"
                SELECT * FROM UserBans
                WHERE HashedIpAddress = @HashedIpAddress AND (ExpiresAt IS NULL OR ExpiresAt > @Now)
                ORDER BY BannedAt DESC LIMIT 1";
            SQLiteParameter[] parameters = [new("@HashedIpAddress", hashedIpAddress), new("@Now", DateTime.UtcNow)];
            DataTable result = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);
            return result.Rows.Count > 0 ? MapDataRowToUserBanGetGet(row: result.Rows[0]) : null;
        }

        public async Task<IEnumerable<UserBanGet>> GetBanHistoryByUserIdAsync(int userId)
        {
            string query = "SELECT * FROM UserBans WHERE UserId = @UserId ORDER BY BannedAt DESC";
            SQLiteParameter[] parameters = [new("@UserId", userId)];
            DataTable result = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);
            List<UserBanGet> list = new();
            foreach (DataRow row in result.Rows)
            {
                list.Add(item: MapDataRowToUserBanGetGet(row: row));
            }

            return list;
        }

        public async Task<IEnumerable<UserBanGet>> GetAllActiveBansAsync()
        {
            string query = "SELECT * FROM UserBans WHERE ExpiresAt IS NULL OR ExpiresAt > @Now ORDER BY BannedAt DESC";
            SQLiteParameter[] parameters = [new("@Now", DateTime.UtcNow)];
            DataTable result = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);
            List<UserBanGet> list = new();
            foreach (DataRow row in result.Rows)
            {
                list.Add(item: MapDataRowToUserBanGetGet(row: row));
            }

            return list;
        }

        public async Task<IEnumerable<UserBanGet>> GetAllBansAsync(string searchTerm = "", int page = 1, int pageSize = 20, string sortColumnName = "", string sortDirection = "asc")
        {
            // Validate and sanitize sort field/direction
            HashSet<string> validSortFields = new()
            { "BannedAt", "ExpiresAt", "Reason" };
            string orderBy = validSortFields.Contains(item: sortColumnName) ? sortColumnName : "BannedAt";
            string direction = sortDirection.Equals("desc", StringComparison.CurrentCultureIgnoreCase) ? "DESC" : "ASC";

            string whereClause = string.IsNullOrWhiteSpace(value: searchTerm)
                ? ""
                : "WHERE Reason LIKE @SearchTerm";

            string query = $@"
                SELECT * FROM UserBans
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
            List<UserBanGet> list = new();
            foreach (DataRow row in result.Rows)
            {
                list.Add(item: MapDataRowToUserBanGetGet(row: row));
            }

            return list;
        }

        public async Task<IEnumerable<UserGet>> GetUsersAsync(string searchTerm, int page, int pageSize, string sortColumnName = "", string sortDirection = "asc")
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
                SELECT * FROM Users
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
            List<UserGet> list = new();
            foreach (DataRow row in result.Rows)
            {
                list.Add(item: MapDataRowToUserGet(row: row));
            }

            return list;
        }

        public async Task<int> GetTotalUsersCountAsync(string searchTerm)
        {
            string query = string.IsNullOrWhiteSpace(value: searchTerm)
                ? "SELECT COUNT(*) FROM Users"
                : "SELECT COUNT(*) FROM Users WHERE Username LIKE @SearchTerm";
            SQLiteParameter[] parameters = string.IsNullOrWhiteSpace(value: searchTerm)
                ? []
                : [new("@SearchTerm", $"%{searchTerm}%")];
            DataTable result = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);
            return Convert.ToInt32(value: result.Rows[0][0]);
        }

        public async Task<bool> UpdateUserTierAsync(UserTierUpdate tierDto)
        {
            string query = "UPDATE Users SET Tier = @Tier WHERE Id_User = @UserId";
            SQLiteParameter[] parameters = [new("@Tier", (int)tierDto.Tier), new("@UserId", tierDto.UserId)];
            int rows = await _sqlClientSingleton.UpdateAsync(query: query, parameters: parameters);
            return rows > 0;
        }

        private UserGet MapDataRowToUserGet(DataRow row)
        {
            const string table = "Users";
            return new UserGet
            {
                Id = row.GetValueOrDefault(_logManagerService, tableName: table, columnName: "Id_User", isRequired: true, converter: Convert.ToInt32),
                Username = row.GetValueOrDefault(_logManagerService, tableName: table, columnName: "Username", isRequired: true, converter: v => v.ToString()!, defaultValue: string.Empty),
                PasswordHash = row.GetValueOrDefault(_logManagerService, tableName: table, columnName: "PasswordHash", isRequired: true, converter: v => v.ToString()!, defaultValue: string.Empty),
                Tier = row.GetValueOrDefault(_logManagerService, tableName: table, columnName: "Tier", isRequired: true, converter: v => (UserTier)Convert.ToInt32(v)),
                RegistrationDate = row.GetValueOrDefault(_logManagerService, tableName: table, columnName: "RegistrationDate", isRequired: true, converter: v => DateTime.Parse(v.ToString()!)),
                IpAddressHistory = row.Table.Columns.Contains("IpAddressHistory")
                    ? row.GetValueOrDefault(_logManagerService, tableName: table, columnName: "IpAddressHistory", isRequired: false, converter: v => v.ToString()!, defaultValue: string.Empty)
                    : string.Empty
            };
        }

        private UserBanGet MapDataRowToUserBanGetGet(DataRow row)
        {
            const string table = "UserBans";
            return new UserBanGet
            {
                Id = row.GetValueOrDefault(_logManagerService, tableName: table, columnName: "Id_UserBan", isRequired: true, converter: Convert.ToInt32),
                UserId = row.Table.Columns.Contains("UserId") && row["UserId"] != DBNull.Value ? Convert.ToInt32(row["UserId"]) : null,
                HashedIpAddress = row.Table.Columns.Contains("HashedIpAddress") && row["HashedIpAddress"] != DBNull.Value ? row["HashedIpAddress"].ToString() : null,
                BannedByUserId = row.GetValueOrDefault(_logManagerService, tableName: table, columnName: "BannedByUserId", isRequired: true, converter: Convert.ToInt32),
                Reason = row.GetValueOrDefault(_logManagerService, tableName: table, columnName: "Reason", isRequired: true, converter: v => v.ToString()!, defaultValue: string.Empty),
                BanType = row.GetValueOrDefault(_logManagerService, tableName: table, columnName: "BanType", isRequired: true, converter: v => (BanType)Convert.ToInt32(v)),
                BannedAt = row.GetValueOrDefault(_logManagerService, tableName: table, columnName: "BannedAt", isRequired: true, converter: Convert.ToDateTime),
                ExpiresAt = row.Table.Columns.Contains("ExpiresAt") && row["ExpiresAt"] != DBNull.Value && DateTime.TryParse(row["ExpiresAt"].ToString(), out DateTime parsedDateTime) ? parsedDateTime : null,
                IsActive = row.Table.Columns.Contains("ExpiresAt") && (row["ExpiresAt"] == DBNull.Value || (DateTime.TryParse(row["ExpiresAt"].ToString(), out DateTime parsedDateTime2) && parsedDateTime2 > DateTime.UtcNow)),
                Properties = new Dictionary<string, string>()
            };
        }

        [GeneratedRegex("^[a-fA-F0-9]{64}$")]
        private static partial Regex MyRegex();
    }
}
