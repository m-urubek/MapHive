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
    }
}