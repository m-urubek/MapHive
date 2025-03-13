using MapHive.Models;
using System.Data;
using System.Data.SQLite;

namespace MapHive.Repositories
{
    public class UserRepository : IUserRepository
    {
        public int CreateUser(User user)
        {
            string query = @"
                INSERT INTO Users (Username, PasswordHash, RegistrationDate, IpAddress, MacAddress, IsTrusted, IsAdmin)
                VALUES (@Username, @PasswordHash, @RegistrationDate, @IpAddress, @MacAddress, @IsTrusted, @IsAdmin)";

            SQLiteParameter[] parameters = new SQLiteParameter[]
            {
                new("@Username", user.Username),
                new("@PasswordHash", user.PasswordHash),
                new("@RegistrationDate", user.RegistrationDate.ToString("yyyy-MM-dd HH:mm:ss")),
                new("@IpAddress", user.IpAddress),
                new("@MacAddress", user.MacAddress),
                new("@IsTrusted", user.IsTrusted ? 1 : 0),
                new("@IsAdmin", user.IsAdmin ? 1 : 0)
            };

            return MainClient.SqlClient.Insert(query, parameters);
        }

        public User? GetUserById(int id)
        {
            string query = "SELECT * FROM Users WHERE Id = @Id";
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

        public bool CheckMacAddressExists(string macAddress)
        {
            string query = "SELECT COUNT(*) FROM Users WHERE MacAddress = @MacAddress";
            SQLiteParameter[] parameters = new SQLiteParameter[] { new("@MacAddress", macAddress) };

            DataTable result = MainClient.SqlClient.Select(query, parameters);

            return Convert.ToInt32(result.Rows[0][0]) > 0;
        }

        public bool IsBlacklisted(string ipAddress, string macAddress)
        {
            string query = @"
                SELECT COUNT(*) FROM Blacklist 
                WHERE IpAddress = @IpAddress OR MacAddress = @MacAddress";

            SQLiteParameter[] parameters = new SQLiteParameter[]
            {
                new("@IpAddress", ipAddress),
                new("@MacAddress", macAddress)
            };

            DataTable result = MainClient.SqlClient.Select(query, parameters);

            return Convert.ToInt32(result.Rows[0][0]) > 0;
        }

        public int AddToBlacklist(BlacklistedAddress blacklistedAddress)
        {
            string query = @"
                INSERT INTO Blacklist (IpAddress, MacAddress, Reason, BlacklistedDate)
                VALUES (@IpAddress, @MacAddress, @Reason, @BlacklistedDate)";

            SQLiteParameter[] parameters = new SQLiteParameter[]
            {
                new("@IpAddress", blacklistedAddress.IpAddress as object ?? DBNull.Value),
                new("@MacAddress", blacklistedAddress.MacAddress as object ?? DBNull.Value),
                new("@Reason", blacklistedAddress.Reason),
                new("@BlacklistedDate", blacklistedAddress.BlacklistedDate.ToString("yyyy-MM-dd HH:mm:ss"))
            };

            return MainClient.SqlClient.Insert(query, parameters);
        }

        private static User MapDataRowToUser(DataRow row)
        {
            return new User
            {
                Id = Convert.ToInt32(row["Id"]),
                Username = row["Username"].ToString() ?? string.Empty,
                PasswordHash = row["PasswordHash"].ToString() ?? string.Empty,
                RegistrationDate = DateTime.Parse(row["RegistrationDate"].ToString() ?? DateTime.MinValue.ToString()),
                IpAddress = row["IpAddress"].ToString() ?? string.Empty,
                MacAddress = row["MacAddress"].ToString() ?? string.Empty,
                IsTrusted = Convert.ToInt32(row["IsTrusted"]) == 1,
                IsAdmin = Convert.ToInt32(row["IsAdmin"]) == 1
            };
        }
    }
}