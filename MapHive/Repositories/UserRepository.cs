using MapHive.Models;
using MapHive.Singletons;
using System.Data;
using System.Data.SQLite;

namespace MapHive.Repositories
{
    public class UserRepository : IUserRepository
    {
        public int CreateUser(User user)
        {
            string query = @"
                INSERT INTO Users (Username, PasswordHash, RegistrationDate, IpAddress, Tier)
                VALUES (@Username, @PasswordHash, @RegistrationDate, @IpAddress, @Tier)";

            SQLiteParameter[] parameters = new SQLiteParameter[]
            {
                new("@Username", user.Username),
                new("@PasswordHash", user.PasswordHash),
                new("@RegistrationDate", user.RegistrationDate.ToString("yyyy-MM-dd HH:mm:ss")),
                new("@IpAddress", user.IpAddress),
                new("@Tier", (int)user.Tier)
            };

            return MainClient.SqlClient.Insert(query, parameters);
        }

        public User? GetUserById(int id)
        {
            string query = "SELECT * FROM Users WHERE Id_User = @Id";
            SQLiteParameter[] parameters = new SQLiteParameter[] { new("@Id", id) };

            DataTable result = MainClient.SqlClient.Select(query, parameters);

            return result.Rows.Count > 0 ? MapDataRowToUser(result.Rows[0]) : null;
        }

        public User? GetUserByUsername(string username)
        {
            string query = "SELECT * FROM Users WHERE Username = @Username";
            SQLiteParameter[] parameters = new SQLiteParameter[] { new("@Username", username) };

            DataTable result = MainClient.SqlClient.Select(query, parameters);

            return result.Rows.Count > 0 ? MapDataRowToUser(result.Rows[0]) : null;
        }

        public bool CheckUsernameExists(string username)
        {
            string query = "SELECT COUNT(*) FROM Users WHERE Username = @Username";
            SQLiteParameter[] parameters = new SQLiteParameter[] { new("@Username", username) };

            DataTable result = MainClient.SqlClient.Select(query, parameters);

            return Convert.ToInt32(result.Rows[0][0]) > 0;
        }

        public bool IsBlacklisted(string ipAddress)
        {
            string query = @"
                SELECT COUNT(*) FROM Blacklist 
                WHERE IpAddress = @IpAddress";

            SQLiteParameter[] parameters = new SQLiteParameter[]
            {
                new("@IpAddress", ipAddress)
            };

            DataTable result = MainClient.SqlClient.Select(query, parameters);

            return Convert.ToInt32(result.Rows[0][0]) > 0;
        }

        public int AddToBlacklist(BlacklistedAddress blacklistedAddress)
        {
            string query = @"
                INSERT INTO Blacklist (IpAddress, Reason, BlacklistedDate)
                VALUES (@IpAddress, @Reason, @BlacklistedDate)";

            SQLiteParameter[] parameters = new SQLiteParameter[]
            {
                new("@IpAddress", blacklistedAddress.IpAddress as object ?? DBNull.Value),
                new("@Reason", blacklistedAddress.Reason),
                new("@BlacklistedDate", blacklistedAddress.BlacklistedDate.ToString("yyyy-MM-dd HH:mm:ss"))
            };

            return MainClient.SqlClient.Insert(query, parameters);
        }

        public void UpdateUser(User user)
        {
            string query = @"
                UPDATE Users 
                SET Username = @Username, 
                    PasswordHash = @PasswordHash, 
                    IpAddress = @IpAddress, 
                    Tier = @Tier
                WHERE Id_User = @Id";

            SQLiteParameter[] parameters = new SQLiteParameter[]
            {
                new("@Id", user.Id),
                new("@Username", user.Username),
                new("@PasswordHash", user.PasswordHash),
                new("@IpAddress", user.IpAddress),
                new("@Tier", (int)user.Tier)
            };

            _ = MainClient.SqlClient.Update(query, parameters);
        }

        public async Task<string> GetUsernameByIdAsync(int userId)
        {
            string query = "SELECT Username FROM Users WHERE Id_User = @Id";
            SQLiteParameter[] parameters = new SQLiteParameter[] { new("@Id", userId) };

            DataTable result = await MainClient.SqlClient.SelectAsync(query, parameters);

            return result.Rows.Count > 0 ? result.Rows[0]["Username"].ToString() ?? "Unknown" : "Unknown";
        }

        private static User MapDataRowToUser(DataRow row)
        {
            return new User
            {
                Id = Convert.ToInt32(row["Id_User"]),
                Username = row["Username"].ToString() ?? string.Empty,
                PasswordHash = row["PasswordHash"].ToString() ?? string.Empty,
                RegistrationDate = DateTime.Parse(row["RegistrationDate"].ToString() ?? DateTime.MinValue.ToString()),
                IpAddress = row["IpAddress"].ToString() ?? string.Empty,
                Tier = (UserTier)Convert.ToInt32(row["Tier"])
            };
        }

        #region Admin Methods

        public async Task<IEnumerable<User>> GetUsersAsync(string searchTerm, int page, int pageSize, string sortField = "", string sortDirection = "asc")
        {
            return await Task.Run(() =>
            {
                List<User> users = new();
                string query;
                SQLiteParameter[] parameters;

                // Calculate offset for pagination
                int offset = (page - 1) * pageSize;

                // Build sort clause
                string sortClause = "ORDER BY Id_User DESC";

                if (!string.IsNullOrEmpty(sortField))
                {
                    string sortColumn = sortField switch
                    {
                        "Id" => "Id_User",
                        "Username" => "Username",
                        "RegistrationDate" => "RegistrationDate",
                        "IpAddress" => "IpAddress",
                        "Tier" => "Tier",
                        _ => "Id_User"
                    };

                    string sortOrder = sortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
                    sortClause = $"ORDER BY {sortColumn} {sortOrder}";
                }

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    // Search by username
                    query = $@"
                        SELECT * FROM Users 
                        WHERE Username LIKE @SearchTerm 
                        {sortClause}
                        LIMIT @PageSize OFFSET @Offset";

                    parameters = new SQLiteParameter[]
                    {
                        new("@SearchTerm", $"%{searchTerm}%"),
                        new("@PageSize", pageSize),
                        new("@Offset", offset)
                    };
                }
                else
                {
                    // Get all users with pagination
                    query = $@"
                        SELECT * FROM Users 
                        {sortClause}
                        LIMIT @PageSize OFFSET @Offset";

                    parameters = new SQLiteParameter[]
                    {
                        new("@PageSize", pageSize),
                        new("@Offset", offset)
                    };
                }

                DataTable dataTable = MainClient.SqlClient.Select(query, parameters);

                foreach (DataRow row in dataTable.Rows)
                {
                    users.Add(MapDataRowToUser(row));
                }

                return users;
            });
        }

        public async Task<int> GetTotalUsersCountAsync(string searchTerm)
        {
            return await Task.Run(() =>
            {
                string query;
                SQLiteParameter[] parameters;

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    // Count users matching search term
                    query = "SELECT COUNT(*) FROM Users WHERE Username LIKE @SearchTerm";
                    parameters = new SQLiteParameter[] { new("@SearchTerm", $"%{searchTerm}%") };
                }
                else
                {
                    // Count all users
                    query = "SELECT COUNT(*) FROM Users";
                    parameters = Array.Empty<SQLiteParameter>();
                }

                DataTable dataTable = MainClient.SqlClient.Select(query, parameters);
                return Convert.ToInt32(dataTable.Rows[0][0]);
            });
        }

        public async Task<bool> UpdateUserTierAsync(int userId, UserTier tier)
        {
            return await Task.Run(() =>
            {
                string query = "UPDATE Users SET Tier = @Tier WHERE Id_User = @Id";

                SQLiteParameter[] parameters = new SQLiteParameter[]
                {
                    new("@Id", userId),
                    new("@Tier", (int)tier)
                };

                int rowsAffected = MainClient.SqlClient.Update(query, parameters);
                return rowsAffected > 0;
            });
        }

        #endregion

        #region Ban Methods

        public async Task<int> BanUserAsync(UserBan ban)
        {
            return await Task.Run(() =>
            {
                string query = @"
                    INSERT INTO UserBans (UserId, IpAddress, BanType, BannedAt, ExpiresAt, Reason, BannedByUserId)
                    VALUES (@UserId, @IpAddress, @BanType, @BannedAt, @ExpiresAt, @Reason, @BannedByUserId)";

                SQLiteParameter[] parameters = new SQLiteParameter[]
                {
                    new("@UserId", ban.UserId.HasValue ? (object)ban.UserId.Value : DBNull.Value),
                    new("@IpAddress", !string.IsNullOrEmpty(ban.IpAddress) ? (object)ban.IpAddress : DBNull.Value),
                    new("@BanType", (int)ban.BanType),
                    new("@BannedAt", ban.BannedAt.ToString("yyyy-MM-dd HH:mm:ss")),
                    new("@ExpiresAt", ban.ExpiresAt.HasValue ? ban.ExpiresAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : (object)DBNull.Value),
                    new("@Reason", ban.Reason),
                    new("@BannedByUserId", ban.BannedByUserId)
                };

                return MainClient.SqlClient.Insert(query, parameters);
            });
        }

        public async Task<bool> UnbanUserAsync(int banId)
        {
            return await Task.Run(() =>
            {
                string query = "DELETE FROM UserBans WHERE Id_UserBan = @BanId";
                SQLiteParameter[] parameters = new SQLiteParameter[] { new("@BanId", banId) };

                int rowsAffected = MainClient.SqlClient.Delete(query, parameters);
                return rowsAffected > 0;
            });
        }

        public async Task<UserBan?> GetActiveBanByUserIdAsync(int userId)
        {
            return await Task.Run(() =>
            {
                // Get only bans that are active (ExpiresAt is null or in the future)
                string query = @"
                    SELECT * FROM UserBans 
                    WHERE UserId = @UserId 
                    AND (ExpiresAt IS NULL OR datetime(ExpiresAt) > datetime('now'))
                    ORDER BY BannedAt DESC LIMIT 1";

                SQLiteParameter[] parameters = new SQLiteParameter[] { new("@UserId", userId) };

                DataTable result = MainClient.SqlClient.Select(query, parameters);
                return result.Rows.Count > 0 ? MapDataRowToUserBan(result.Rows[0]) : null;
            });
        }

        public async Task<UserBan?> GetActiveBanByIpAddressAsync(string ipAddress)
        {
            return await Task.Run(() =>
            {
                // Get only bans that are active (ExpiresAt is null or in the future)
                string query = @"
                    SELECT * FROM UserBans 
                    WHERE IpAddress = @IpAddress 
                    AND (ExpiresAt IS NULL OR datetime(ExpiresAt) > datetime('now'))
                    ORDER BY BannedAt DESC LIMIT 1";

                SQLiteParameter[] parameters = new SQLiteParameter[] { new("@IpAddress", ipAddress) };

                DataTable result = MainClient.SqlClient.Select(query, parameters);
                return result.Rows.Count > 0 ? MapDataRowToUserBan(result.Rows[0]) : null;
            });
        }

        public async Task<IEnumerable<UserBan>> GetBanHistoryByUserIdAsync(int userId)
        {
            return await Task.Run(() =>
            {
                List<UserBan> bans = new();

                string query = @"
                    SELECT ub.*, u.Username as BannedByUsername 
                    FROM UserBans ub
                    LEFT JOIN Users u ON ub.BannedByUserId = u.Id_User
                    WHERE ub.UserId = @UserId 
                    ORDER BY ub.BannedAt DESC";

                SQLiteParameter[] parameters = new SQLiteParameter[] { new("@UserId", userId) };

                DataTable result = MainClient.SqlClient.Select(query, parameters);

                foreach (DataRow row in result.Rows)
                {
                    bans.Add(MapDataRowToUserBan(row));
                }

                return bans;
            });
        }

        public async Task<IEnumerable<UserBan>> GetAllActiveBansAsync()
        {
            return await Task.Run(() =>
            {
                List<UserBan> bans = new();

                string query = @"
                    SELECT ub.*, u.Username as BannedByUsername 
                    FROM UserBans ub
                    LEFT JOIN Users u ON ub.BannedByUserId = u.Id_User
                    WHERE ExpiresAt IS NULL OR datetime(ExpiresAt) > datetime('now')
                    ORDER BY ub.BannedAt DESC";

                DataTable result = MainClient.SqlClient.Select(query);

                foreach (DataRow row in result.Rows)
                {
                    bans.Add(MapDataRowToUserBan(row));
                }

                return bans;
            });
        }

        private static UserBan MapDataRowToUserBan(DataRow row)
        {
            return new UserBan
            {
                Id = Convert.ToInt32(row["Id_UserBan"]),
                UserId = row["UserId"] != DBNull.Value ? Convert.ToInt32(row["UserId"]) : null,
                IpAddress = row["IpAddress"] != DBNull.Value ? row["IpAddress"].ToString() : null,
                BanType = (BanType)Convert.ToInt32(row["BanType"]),
                BannedAt = DateTime.Parse(row["BannedAt"].ToString() ?? DateTime.MinValue.ToString()),
                ExpiresAt = row["ExpiresAt"] != DBNull.Value
                    ? DateTime.Parse(row["ExpiresAt"].ToString() ?? DateTime.MaxValue.ToString())
                    : null,
                Reason = row["Reason"].ToString() ?? string.Empty,
                BannedByUserId = Convert.ToInt32(row["BannedByUserId"])
            };
        }

        #endregion
    }
}