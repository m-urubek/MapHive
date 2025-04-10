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
                INSERT INTO Users (Username, PasswordHash, RegistrationDate, Tier, IpAddressHistory)
                VALUES (@Username, @PasswordHash, @RegistrationDate, @Tier, @IpAddressHistory)";

            SQLiteParameter[] parameters = new SQLiteParameter[]
            {
                new("@Username", user.Username),
                new("@PasswordHash", user.PasswordHash),
                new("@RegistrationDate", user.RegistrationDate.ToString("yyyy-MM-dd HH:mm:ss")),
                new("@Tier", (int)user.Tier),
                new("@IpAddressHistory", user.IpAddressHistory)
            };

            return CurrentRequest.SqlClient.Insert(query, parameters);
        }

        public User? GetUserById(int id)
        {
            string query = "SELECT * FROM Users WHERE Id_User = @Id";
            SQLiteParameter[] parameters = new SQLiteParameter[] { new("@Id", id) };

            DataTable result = CurrentRequest.SqlClient.Select(query, parameters);

            return result.Rows.Count > 0 ? MapDataRowToUser(result.Rows[0]) : null;
        }

        public User? GetUserByUsername(string username)
        {
            string query = "SELECT * FROM Users WHERE Username = @Username";
            SQLiteParameter[] parameters = new SQLiteParameter[] { new("@Username", username) };

            DataTable result = CurrentRequest.SqlClient.Select(query, parameters);

            return result.Rows.Count > 0 ? MapDataRowToUser(result.Rows[0]) : null;
        }

        public bool CheckUsernameExists(string username)
        {
            string query = "SELECT COUNT(*) FROM Users WHERE Username = @Username";
            SQLiteParameter[] parameters = new SQLiteParameter[] { new("@Username", username) };

            DataTable result = CurrentRequest.SqlClient.Select(query, parameters);

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

            DataTable result = CurrentRequest.SqlClient.Select(query, parameters);

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

            return CurrentRequest.SqlClient.Insert(query, parameters);
        }

        public void UpdateUser(User user)
        {
            string query = @"
                UPDATE Users 
                SET Username = @Username, 
                    PasswordHash = @PasswordHash, 
                    Tier = @Tier,
                    IpAddressHistory = @IpAddressHistory
                WHERE Id_User = @Id";

            SQLiteParameter[] parameters = new SQLiteParameter[]
            {
                new("@Id", user.Id),
                new("@Username", user.Username),
                new("@PasswordHash", user.PasswordHash),
                new("@Tier", (int)user.Tier),
                new("@IpAddressHistory", user.IpAddressHistory)
            };

            _ = CurrentRequest.SqlClient.Update(query, parameters);
        }

        public async Task<string> GetUsernameByIdAsync(int userId)
        {
            string query = "SELECT Username FROM Users WHERE Id_User = @Id";
            SQLiteParameter[] parameters = new SQLiteParameter[] { new("@Id", userId) };

            DataTable result = await CurrentRequest.SqlClient.SelectAsync(query, parameters);

            return result.Rows.Count > 0 ? result.Rows[0]["Username"].ToString() ?? "Unknown" : "Unknown";
        }

        private static User MapDataRowToUser(DataRow row)
        {
            User user = new()
            {
                Id = Convert.ToInt32(row["Id_User"]),
                Username = row["Username"].ToString() ?? string.Empty,
                PasswordHash = row["PasswordHash"].ToString() ?? string.Empty,
                RegistrationDate = DateTime.Parse(row["RegistrationDate"].ToString() ?? DateTime.MinValue.ToString()),
                Tier = (UserTier)Convert.ToInt32(row["Tier"]),
                IpAddressHistory = row.Table.Columns.Contains("IpAddressHistory") && row["IpAddressHistory"] != DBNull.Value
                    ? row["IpAddressHistory"].ToString() ?? string.Empty
                    : string.Empty
            };

            return user;
        }

        #region Admin Methods

        public async Task<IEnumerable<User>> GetUsersAsync(string searchTerm, int page, int pageSize, string sortField = "", string sortDirection = "asc")
        {
            List<User> users = new();
            string query;
            SQLiteParameter[] parameters;

            // Calculate offset for pagination
            int offset = (page - 1) * pageSize;

            // Build sort clause
            string sortClause = "ORDER BY Id_User DESC"; // Default sort

            if (!string.IsNullOrEmpty(sortField))
            {
                string sortColumn = sortField switch
                {
                    "Id" => "Id_User",
                    "Username" => "Username",
                    "RegistrationDate" => "RegistrationDate",
                    "Tier" => "Tier",
                    _ => "Id_User" // Default to Id_User if sortField is unknown
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

            // Use SelectAsync directly
            DataTable dataTable = await CurrentRequest.SqlClient.SelectAsync(query, parameters);

            foreach (DataRow row in dataTable.Rows)
            {
                users.Add(MapDataRowToUser(row));
            }

            return users;
        }

        public async Task<int> GetTotalUsersCountAsync(string searchTerm)
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

            // Use SelectAsync directly
            DataTable dataTable = await CurrentRequest.SqlClient.SelectAsync(query, parameters);
            return Convert.ToInt32(dataTable.Rows[0][0]);
        }

        public async Task<bool> UpdateUserTierAsync(int userId, UserTier tier)
        {
            string query = "UPDATE Users SET Tier = @Tier WHERE Id_User = @Id";

            SQLiteParameter[] parameters = new SQLiteParameter[]
            {
                    new("@Id", userId),
                    new("@Tier", (int)tier)
            };

            // Use UpdateAsync directly
            int rowsAffected = await CurrentRequest.SqlClient.UpdateAsync(query, parameters);
            return rowsAffected > 0;
        }

        #endregion

        #region Ban Methods

        public async Task<int> BanUserAsync(UserBan ban)
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

            // Use InsertAsync directly
            return await CurrentRequest.SqlClient.InsertAsync(query, parameters);
        }

        public async Task<bool> UnbanUserAsync(int banId)
        {
            string query = "DELETE FROM UserBans WHERE Id_UserBan = @BanId";
            SQLiteParameter[] parameters = new SQLiteParameter[] { new("@BanId", banId) };

            // Use DeleteAsync directly
            int rowsAffected = await CurrentRequest.SqlClient.DeleteAsync(query, parameters);
            return rowsAffected > 0;
        }

        public async Task<UserBan?> GetActiveBanByUserIdAsync(int userId)
        {
            // Get only bans that are active (ExpiresAt is null or in the future)
            string query = @"
                    SELECT * FROM UserBans
                    WHERE UserId = @UserId
                    AND (ExpiresAt IS NULL OR datetime(ExpiresAt) > datetime('now'))
                    ORDER BY BannedAt DESC LIMIT 1";

            SQLiteParameter[] parameters = new SQLiteParameter[] { new("@UserId", userId) };

            // Use SelectAsync directly
            DataTable result = await CurrentRequest.SqlClient.SelectAsync(query, parameters);

            if (result.Rows.Count > 0)
            {
                UserBan ban = MapDataRowToUserBan(result.Rows[0]);
                return ban;
            }
            return null;
        }

        public async Task<UserBan?> GetActiveBanByIpAddressAsync(string ipAddress)
        {
            // Get only bans that are active (ExpiresAt is null or in the future)
            string query = @"
                    SELECT * FROM UserBans
                    WHERE IpAddress = @IpAddress
                    AND (ExpiresAt IS NULL OR datetime(ExpiresAt) > datetime('now'))
                    ORDER BY BannedAt DESC LIMIT 1";

            SQLiteParameter[] parameters = new SQLiteParameter[] { new("@IpAddress", ipAddress) };

            // Use SelectAsync directly
            DataTable result = await CurrentRequest.SqlClient.SelectAsync(query, parameters);

            if (result.Rows.Count > 0)
            {
                UserBan ban = MapDataRowToUserBan(result.Rows[0]);
                return ban;
            }
            return null;
        }

        public async Task<IEnumerable<UserBan>> GetBanHistoryByUserIdAsync(int userId)
        {
            List<UserBan> bans = new();
            string query = @"
                    SELECT * FROM UserBans
                    WHERE UserId = @UserId
                    ORDER BY BannedAt DESC";

            SQLiteParameter[] parameters = new SQLiteParameter[] { new("@UserId", userId) };

            // Use SelectAsync directly
            DataTable result = await CurrentRequest.SqlClient.SelectAsync(query, parameters);

            foreach (DataRow row in result.Rows)
            {
                UserBan ban = MapDataRowToUserBan(row);
                bans.Add(ban);
            }
            return bans;
        }

        public async Task<IEnumerable<UserBan>> GetAllActiveBansAsync()
        {
            List<UserBan> bans = new();
            // Get only bans that are active (ExpiresAt is null or in the future)
            string query = @"
                    SELECT * FROM UserBans
                    WHERE ExpiresAt IS NULL OR datetime(ExpiresAt) > datetime('now')
                    ORDER BY BannedAt DESC";

            // Use SelectAsync directly
            DataTable result = await CurrentRequest.SqlClient.SelectAsync(query);

            foreach (DataRow row in result.Rows)
            {
                UserBan ban = MapDataRowToUserBan(row);
                bans.Add(ban);
            }
            return bans;
        }

        public async Task<IEnumerable<UserBan>> GetAllBansAsync(string searchTerm = "", int page = 1, int pageSize = 20, string sortField = "", string sortDirection = "asc")
        {
            List<UserBan> bans = new();
            List<SQLiteParameter> parametersList = new();
            string whereClause = "";
            string sortClause = "ORDER BY BannedAt DESC"; // Default sort

            // Build search condition
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                whereClause = "WHERE (Reason LIKE @SearchTerm OR IpAddress LIKE @SearchTerm)";
                parametersList.Add(new SQLiteParameter("@SearchTerm", $"%{searchTerm}%"));

                // Attempt to search by User ID if the search term is a valid integer
                if (int.TryParse(searchTerm, out int searchUserId))
                {
                    whereClause += " OR UserId = @SearchUserId";
                    parametersList.Add(new SQLiteParameter("@SearchUserId", searchUserId));
                }
                // Attempt to search by BannedByUserId if the search term is a valid integer
                if (int.TryParse(searchTerm, out int searchBannedByUserId))
                {
                    whereClause += " OR BannedByUserId = @SearchBannedByUserId";
                    parametersList.Add(new SQLiteParameter("@SearchBannedByUserId", searchBannedByUserId));
                }

                // Attempt to search by Username (requires join) or BannedByUsername (requires join)
                // This part is more complex and might require joining with the Users table.
                // For simplicity in this example, we'll stick to direct fields or add joins later if needed.
                // Example placeholder for joining:
                // query = @"SELECT b.*, u.Username as BannedUsername, admin.Username as BannedByUsername
                //           FROM UserBans b
                //           LEFT JOIN Users u ON b.UserId = u.Id_User
                //           LEFT JOIN Users admin ON b.BannedByUserId = admin.Id_User
                //           WHERE ...";
                // If implementing search by username, the query and parameter handling need adjustment.
            }

            // Build sort clause
            if (!string.IsNullOrEmpty(sortField))
            {
                string sortColumn = sortField switch
                {
                    "Id" => "Id_UserBan",
                    "UserId" => "UserId",
                    "IpAddress" => "IpAddress",
                    "BanType" => "BanType",
                    "BannedAt" => "BannedAt",
                    "ExpiresAt" => "ExpiresAt",
                    "Reason" => "Reason",
                    "BannedByUserId" => "BannedByUserId",
                    _ => "BannedAt" // Default to BannedAt if sortField is unknown
                };

                string sortOrder = sortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
                sortClause = $"ORDER BY {sortColumn} {sortOrder}";
            }

            // Calculate offset for pagination
            int offset = (page - 1) * pageSize;

            string query = $@"
                    SELECT * FROM UserBans
                    {whereClause}
                    {sortClause}
                    LIMIT @PageSize OFFSET @Offset";

            // Add pagination parameters
            parametersList.Add(new SQLiteParameter("@PageSize", pageSize));
            parametersList.Add(new SQLiteParameter("@Offset", offset));

            // Use SelectAsync directly
            DataTable result = await CurrentRequest.SqlClient.SelectAsync(query, parametersList.ToArray());

            foreach (DataRow row in result.Rows)
            {
                UserBan ban = MapDataRowToUserBan(row);

                bans.Add(ban);
            }

            return bans;
        }

        public async Task<int> GetTotalBansCountAsync(string searchTerm = "")
        {
            string query;
            List<SQLiteParameter> parametersList = new();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                // Base query for searching
                query = "SELECT COUNT(*) FROM UserBans WHERE (Reason LIKE @SearchTerm OR IpAddress LIKE @SearchTerm)";
                parametersList.Add(new SQLiteParameter("@SearchTerm", $"%{searchTerm}%"));

                // Add user ID search if applicable
                if (int.TryParse(searchTerm, out int searchUserId))
                {
                    query += " OR UserId = @SearchUserId";
                    parametersList.Add(new SQLiteParameter("@SearchUserId", searchUserId));
                }
                // Add banned by user ID search if applicable
                if (int.TryParse(searchTerm, out int searchBannedByUserId))
                {
                    query += " OR BannedByUserId = @SearchBannedByUserId";
                    parametersList.Add(new SQLiteParameter("@SearchBannedByUserId", searchBannedByUserId));
                }
                // Add username search if needed (requires joins)
            }
            else
            {
                // Count all bans
                query = "SELECT COUNT(*) FROM UserBans";
            }

            // Use SelectAsync directly
            DataTable dataTable = await CurrentRequest.SqlClient.SelectAsync(query, parametersList.ToArray());
            return Convert.ToInt32(dataTable.Rows[0][0]);
        }

        public async Task<UserBan?> GetBanByIdAsync(int banId)
        {
            string query = "SELECT * FROM UserBans WHERE Id_UserBan = @BanId";
            SQLiteParameter[] parameters = new SQLiteParameter[] { new("@BanId", banId) };

            // Use SelectAsync directly
            DataTable result = await CurrentRequest.SqlClient.SelectAsync(query, parameters);

            if (result.Rows.Count > 0)
            {
                UserBan ban = MapDataRowToUserBan(result.Rows[0]);
                return ban;
            }
            return null;
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