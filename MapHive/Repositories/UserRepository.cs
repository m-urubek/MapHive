using MapHive.Models.Enums;
using MapHive.Models.Exceptions;
using MapHive.Models.RepositoryModels;
using MapHive.Singletons;
using System.Data;
using System.Data.SQLite;
using System.Text.RegularExpressions;

namespace MapHive.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ISqlClientSingleton _sqlClient;
        private readonly ILogManagerSingleton _logManager;

        public UserRepository(ISqlClientSingleton sqlClient, ILogManagerSingleton logManager)
        {
            this._sqlClient = sqlClient;
            this._logManager = logManager;
        }

        // Create a new user using DTO
        public async Task<int> CreateUserAsync(UserCreate createDto)
        {
            string query = @"
                INSERT INTO Users (Username, PasswordHash, RegistrationDate, Tier, IpAddressHistory)
                VALUES (@Username, @PasswordHash, @RegistrationDate, @Tier, @IpAddressHistory);";

            SQLiteParameter[] parameters = new SQLiteParameter[]
            {
                new("@Username", createDto.Username),
                new("@PasswordHash", createDto.PasswordHash),
                new("@RegistrationDate", createDto.RegistrationDate.ToString("yyyy-MM-dd HH:mm:ss")),
                new("@Tier", (int)createDto.Tier),
                new("@IpAddressHistory", createDto.IpAddressHistory)
            };

            return await this._sqlClient.InsertAsync(query, parameters);
        }

        // Basic user lookup
        public async Task<UserGet?> GetUserByIdAsync(int id)
        {
            string query = "SELECT * FROM Users WHERE Id_User = @Id_Log";
            SQLiteParameter[] parameters = new SQLiteParameter[] { new("@Id_Log", id) };
            DataTable result = await this._sqlClient.SelectAsync(query, parameters);
            return result.Rows.Count > 0 ? MapDataRowToUserGet(result.Rows[0]) : null;
        }

        public async Task<UserGet?> GetUserByUsernameAsync(string username)
        {
            string query = "SELECT * FROM Users WHERE Username = @Username";
            SQLiteParameter[] parameters = new SQLiteParameter[] { new("@Username", username) };
            DataTable result = await this._sqlClient.SelectAsync(query, parameters);

            if (result.Rows.Count == 0)
            {
                throw new OrangeUserException($"User \"{username}\" not found");
            }

            UserGet user = MapDataRowToUserGet(result.Rows[0]);

            // Log password hash length for debugging
            if (string.IsNullOrEmpty(user.PasswordHash))
            {
                this._logManager.Warning($"User {username} found but has empty password hash");
            }

            return user;
        }

        public async Task<bool> CheckUsernameExistsAsync(string username)
        {
            string query = "SELECT 1 FROM Users WHERE Username = @Username LIMIT 1";
            SQLiteParameter[] parameters = new SQLiteParameter[] { new("@Username", username) };
            DataTable result = await this._sqlClient.SelectAsync(query, parameters);
            return result.Rows.Count > 0;
        }

        public async Task<bool> IsIpBannedAsync(string hashedIpAddress)
        {
            string query = @"
                SELECT 1 FROM IpBans
                WHERE HashedIpAddress = @HashedIpAddress
                  AND (ExpiresAt IS NULL OR ExpiresAt > @Now)
                LIMIT 1";
            SQLiteParameter[] parameters = new SQLiteParameter[]
            {
                new("@HashedIpAddress", hashedIpAddress),
                new("@Now", DateTime.UtcNow)
            };
            DataTable result = await this._sqlClient.SelectAsync(query, parameters);
            return result.Rows.Count > 0;
        }

        public async Task<int> CreateIpBanAsync(IpBanCreate ipBan)
        {
            // Basic validation for SHA256 hash format
            if (string.IsNullOrEmpty(ipBan.IpAddress) || !Regex.IsMatch(ipBan.IpAddress, "^[a-fA-F0-9]{64}$"))
            {
                this._logManager.Warning($"Attempted to ban an invalid or non-hashed IP format: {ipBan.IpAddress}");
                throw new ArgumentException("IP address must be a valid SHA256 hash.");
            }

            string query = @"
                INSERT INTO IpBans (HashedIpAddress, Reason, BannedAt, ExpiresAt, BannedByUserId)
                VALUES (@HashedIpAddress, @Reason, @BannedAt, @ExpiresAt, @BannedByUserId);";
            SQLiteParameter[] parameters = new SQLiteParameter[]
            {
                new("@HashedIpAddress", ipBan.IpAddress),
                new("@Reason", ipBan.Reason),
                new("@BannedAt", ipBan.BannedAt.ToString("yyyy-MM-dd HH:mm:ss")),
                new("@ExpiresAt", ipBan.ExpiresAt.HasValue ? ipBan.ExpiresAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : (object)DBNull.Value),
                new("@BannedByUserId", ipBan.BannedByUserId)
            };
            return await this._sqlClient.InsertAsync(query, parameters);
        }

        public async Task<int> UpdateUserAsync(UserUpdate updateDto)
        {
            string query = @"
                UPDATE Users
                SET Username = @Username,
                    PasswordHash = @PasswordHash,
                    Tier = @Tier,
                    IpAddressHistory = @IpAddressHistory
                WHERE Id_User = @Id_Log";

            SQLiteParameter[] parameters = new SQLiteParameter[]
            {
                new("@Id_Log", updateDto.Id),
                new("@Username", updateDto.Username),
                new("@PasswordHash", updateDto.PasswordHash as object ?? DBNull.Value),
                new("@Tier", (int)updateDto.Tier),
                new("@IpAddressHistory", updateDto.IpAddressHistory)
            };
            return await this._sqlClient.UpdateAsync(query, parameters);
        }

        public async Task<string> GetUsernameByIdAsync(int userId)
        {
            string query = "SELECT Username FROM Users WHERE Id_User = @Id_Log";
            SQLiteParameter[] parameters = new SQLiteParameter[] { new("@Id_Log", userId) };
            DataTable result = await this._sqlClient.SelectAsync(query, parameters);
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
            SQLiteParameter[] parameters = new SQLiteParameter[]
            {
                new("@UserId", banDto.UserId as object ?? DBNull.Value),
                new("@HashedIpAddress", banDto.HashedIpAddress as object ?? DBNull.Value),
                new("@BanType", (int)banDto.BanType),
                new("@BannedAt", banDto.BannedAt),
                new("@ExpiresAt", banDto.ExpiresAt as object ?? DBNull.Value),
                new("@Reason", banDto.Reason),
                new("@BannedByUserId", banDto.BannedByUserId)
            };
            return await this._sqlClient.InsertAsync(query, parameters);
        }

        public async Task<bool> UnbanUserAsync(int banId)
        {
            string query = "UPDATE UserBans SET ExpiresAt = @ExpiresAt WHERE Id_UserBan = @Id_UserBan";
            SQLiteParameter[] parameters = new SQLiteParameter[]
            {
                new("@ExpiresAt", DateTime.UtcNow),
                new("@Id_UserBan", banId)
            };
            int rows = await this._sqlClient.UpdateAsync(query, parameters);
            return rows > 0;
        }

        public async Task<UserBanGet?> GetActiveBanByUserIdAsync(int userId)
        {
            string query = @"
                SELECT * FROM UserBans
                WHERE UserId = @UserId AND (ExpiresAt IS NULL OR ExpiresAt > @Now)
                ORDER BY BannedAt DESC LIMIT 1";
            SQLiteParameter[] parameters = new SQLiteParameter[] { new("@UserId", userId), new("@Now", DateTime.UtcNow) };
            DataTable result = await this._sqlClient.SelectAsync(query, parameters);
            return result.Rows.Count > 0 ? MapDataRowToUserBanGetGet(result.Rows[0]) : null;
        }

        public async Task<UserBanGet?> GetActiveBanByIpAddressAsync(string hashedIpAddress)
        {
            string query = @"
                SELECT * FROM UserBans
                WHERE HashedIpAddress = @HashedIpAddress AND (ExpiresAt IS NULL OR ExpiresAt > @Now)
                ORDER BY BannedAt DESC LIMIT 1";
            SQLiteParameter[] parameters = new SQLiteParameter[] { new("@HashedIpAddress", hashedIpAddress), new("@Now", DateTime.UtcNow) };
            DataTable result = await this._sqlClient.SelectAsync(query, parameters);
            return result.Rows.Count > 0 ? MapDataRowToUserBanGetGet(result.Rows[0]) : null;
        }

        public async Task<IEnumerable<UserBanGet>> GetBanHistoryByUserIdAsync(int userId)
        {
            string query = "SELECT * FROM UserBans WHERE UserId = @UserId ORDER BY BannedAt DESC";
            SQLiteParameter[] parameters = new SQLiteParameter[] { new("@UserId", userId) };
            DataTable result = await this._sqlClient.SelectAsync(query, parameters);
            List<UserBanGet> list = new();
            foreach (DataRow row in result.Rows)
            {
                list.Add(MapDataRowToUserBanGetGet(row));
            }

            return list;
        }

        public async Task<IEnumerable<UserBanGet>> GetAllActiveBansAsync()
        {
            string query = "SELECT * FROM UserBans WHERE ExpiresAt IS NULL OR ExpiresAt > @Now ORDER BY BannedAt DESC";
            SQLiteParameter[] parameters = new SQLiteParameter[] { new("@Now", DateTime.UtcNow) };
            DataTable result = await this._sqlClient.SelectAsync(query, parameters);
            List<UserBanGet> list = new();
            foreach (DataRow row in result.Rows)
            {
                list.Add(MapDataRowToUserBanGetGet(row));
            }

            return list;
        }

        public async Task<IEnumerable<UserBanGet>> GetAllBansAsync(string searchTerm = "", int page = 1, int pageSize = 20, string sortField = "", string sortDirection = "asc")
        {
            // Validate and sanitize sort field/direction
            HashSet<string> validSortFields = new()
            { "BannedAt", "ExpiresAt", "Reason" };
            string orderBy = validSortFields.Contains(sortField) ? sortField : "BannedAt";
            string direction = sortDirection.ToLower() == "desc" ? "DESC" : "ASC";

            string whereClause = string.IsNullOrWhiteSpace(searchTerm)
                ? ""
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

            DataTable result = await this._sqlClient.SelectAsync(query, parametersList.ToArray());
            List<UserBanGet> list = new();
            foreach (DataRow row in result.Rows)
            {
                list.Add(MapDataRowToUserBanGetGet(row));
            }

            return list;
        }

        public async Task<IEnumerable<UserGet>> GetUsersAsync(string searchTerm, int page, int pageSize, string sortField = "", string sortDirection = "asc")
        {
            // Validate and sanitize sort field/direction
            HashSet<string> validSortFields = new()
            { "Username", "RegistrationDate", "Tier" };
            string orderBy = validSortFields.Contains(sortField) ? sortField : "Username";
            string direction = sortDirection.ToLower() == "desc" ? "DESC" : "ASC";

            string whereClause = string.IsNullOrWhiteSpace(searchTerm)
                ? ""
                : "WHERE Username LIKE @SearchTerm";

            string query = $@"
                SELECT * FROM Users
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

            DataTable result = await this._sqlClient.SelectAsync(query, parametersList.ToArray());
            List<UserGet> list = new();
            foreach (DataRow row in result.Rows)
            {
                list.Add(MapDataRowToUserGet(row));
            }

            return list;
        }

        public async Task<int> GetTotalUsersCountAsync(string searchTerm)
        {
            string query = string.IsNullOrWhiteSpace(searchTerm)
                ? "SELECT COUNT(*) FROM Users"
                : "SELECT COUNT(*) FROM Users WHERE Username LIKE @SearchTerm";
            SQLiteParameter[] parameters = string.IsNullOrWhiteSpace(searchTerm)
                ? Array.Empty<SQLiteParameter>()
                : new SQLiteParameter[] { new("@SearchTerm", $"%{searchTerm}%") };
            DataTable result = await this._sqlClient.SelectAsync(query, parameters);
            return Convert.ToInt32(result.Rows[0][0]);
        }

        public async Task<bool> UpdateUserTierAsync(UserTierUpdate tierDto)
        {
            string query = "UPDATE Users SET Tier = @Tier WHERE Id_User = @UserId";
            SQLiteParameter[] parameters = new SQLiteParameter[] { new("@Tier", (int)tierDto.Tier), new("@UserId", tierDto.UserId) };
            int rows = await this._sqlClient.UpdateAsync(query, parameters);
            return rows > 0;
        }

        private static UserGet MapDataRowToUserGet(DataRow row)
        {
            return new UserGet
            {
                Id = Convert.ToInt32(row["Id_User"]),
                Username = row["Username"].ToString() ?? string.Empty,
                PasswordHash = row["PasswordHash"].ToString() ?? string.Empty,
                Tier = row["Tier"] != DBNull.Value ? (UserTier)Convert.ToInt32(row["Tier"]) : default,
                RegistrationDate = DateTime.Parse(row["RegistrationDate"].ToString() ?? DateTime.MinValue.ToString()),
                IpAddressHistory = row.Table.Columns.Contains("IpAddressHistory") && row["IpAddressHistory"] != DBNull.Value
                    ? row["IpAddressHistory"].ToString()!
                    : string.Empty
            };
        }

        private static UserBanGet MapDataRowToUserBanGetGet(DataRow row)
        {
            return new UserBanGet
            {
                Id = Convert.ToInt32(row["Id_UserBan"]),
                UserId = row.Table.Columns.Contains("UserId") && row["UserId"] != DBNull.Value ? Convert.ToInt32(row["UserId"]) : (int?)null,
                HashedIpAddress = row.Table.Columns.Contains("HashedIpAddress") && row["HashedIpAddress"] != DBNull.Value ? row["HashedIpAddress"].ToString() : null,
                BannedByUserId = Convert.ToInt32(row["BannedByUserId"]),
                Reason = row["Reason"].ToString() ?? string.Empty,
                BanType = (BanType)Convert.ToInt32(row["BanType"]),
                BannedAt = Convert.ToDateTime(row["BannedAt"]),
                ExpiresAt = row.Table.Columns.Contains("ExpiresAt") && row["ExpiresAt"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["ExpiresAt"]) : null,
                IsActive = row.Table.Columns.Contains("ExpiresAt") && (row["ExpiresAt"] == DBNull.Value || Convert.ToDateTime(row["ExpiresAt"]) > DateTime.UtcNow),
                Properties = new Dictionary<string, string>()
            };
        }
    }
}