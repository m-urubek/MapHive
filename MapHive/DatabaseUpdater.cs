using MapHive;
using System.Data;
using System.Data.SQLite;
using System.Reflection;

namespace SuperSmashHoes
{
    public static class DatabaseUpdater
    {
        public static void Run()
        {
            DataTable versionRawData = MainClient.SqlClient.Select("select * from VersionNumber");
            if (versionRawData.Rows.Count == 0)
            {
                throw new Exception("Version data has no rows");
            }

            int dbVersion = versionRawData.Rows[0]["Value"] == null || string.IsNullOrWhiteSpace(versionRawData.Rows[0]["Value"].ToString())
                ? 0
                : int.Parse(versionRawData.Rows[0]["Value"].ToString());
            MethodInfo[] versionMethods = typeof(DatabaseUpdater).GetMethods(BindingFlags.Public | BindingFlags.Static);
            int lastUpdateNumber = dbVersion;
            foreach (MethodInfo method in versionMethods)
            {
                if (method.Name == "Run")
                {
                    continue;
                }

                int methodVersion = int.Parse(method.Name.Remove(0, 1));
                if (methodVersion > dbVersion)
                {
                    _ = method.Invoke(null, null);
                }

                lastUpdateNumber = methodVersion;
            }
            if (lastUpdateNumber > dbVersion)
            {
                string query = "UPDATE VersionNumber SET Value = @Value WHERE Id = @Id";
                SQLiteParameter[] parameters = new SQLiteParameter[]
                {
                new("@Value", lastUpdateNumber),
                new("@Id", versionRawData.Rows[0]["Id"])
                };
                if (MainClient.SqlClient.Update(query, parameters) != 1)
                {
                    throw new Exception("Unable to update database number");
                }
            }
        }

        public static void v1()
        {
            _ = MainClient.SqlClient.Alter(@"CREATE TABLE IF NOT EXISTS 'MapLocations' (
                'Id'	INTEGER,
                'Name'	TEXT NOT NULL,
                'Description'	TEXT NOT NULL,
                'Latitude'	REAL NOT NULL,
                'Longitude'	REAL NOT NULL,
                'Address'	TEXT,
                'Website'	TEXT,
                'PhoneNumber'	TEXT,
                'CreatedAt'	TEXT NOT NULL,
                'UpdatedAt'	TEXT NOT NULL,
                PRIMARY KEY('Id' AUTOINCREMENT)
            )");
        }

        public static void v2()
        {
            // Create LogSeverity table to store different log levels
            _ = MainClient.SqlClient.Alter(@"CREATE TABLE IF NOT EXISTS 'LogSeverity' (
                'Id'	INTEGER,
                'Name'	TEXT NOT NULL,
                'Description' TEXT,
                PRIMARY KEY('Id' AUTOINCREMENT)
            )");

            // Insert default severity levels - only 4 levels as requested
            string[] severityLevels = new string[] { "Information", "Warning", "Error", "Critical" };
            string[] descriptions = new string[] {
                "General information messages",
                "Warnings that don't cause application failure",
                "Errors that affect the current operation",
                "Critical errors that cause application failure"
            };

            for (int i = 0; i < severityLevels.Length; i++)
            {
                _ = MainClient.SqlClient.Insert(
                    "INSERT INTO LogSeverity (Name, Description) VALUES (@Name, @Description);",
                    new SQLiteParameter[] {
                        new("@Name", severityLevels[i]),
                        new("@Description", descriptions[i])
                    }
                );
            }

            // Create Logs table
            _ = MainClient.SqlClient.Alter(@"CREATE TABLE IF NOT EXISTS 'Logs' (
                'Id'	INTEGER,
                'Timestamp' TEXT NOT NULL,
                'SeverityId' INTEGER NOT NULL,
                'Message' TEXT NOT NULL,
                'Source' TEXT,
                'Exception' TEXT,
                'UserName' TEXT,
                'RequestPath' TEXT,
                'AdditionalData' TEXT,
                PRIMARY KEY('Id' AUTOINCREMENT),
                FOREIGN KEY('SeverityId') REFERENCES 'LogSeverity'('Id')
            )");
        }

        public static void v3()
        {
            // Create Users table
            _ = MainClient.SqlClient.Alter(@"CREATE TABLE IF NOT EXISTS 'Users' (
                'Id'	INTEGER,
                'Username'	TEXT NOT NULL UNIQUE,
                'PasswordHash'	TEXT NOT NULL,
                'RegistrationDate'	TEXT NOT NULL,
                'IpAddress'	TEXT NOT NULL,
                'MacAddress'	TEXT NOT NULL,
                'IsTrusted'	INTEGER NOT NULL DEFAULT 0,
                'IsAdmin'	INTEGER NOT NULL DEFAULT 0,
                PRIMARY KEY('Id' AUTOINCREMENT)
            )");

            // Create blacklist table for IP and MAC addresses
            _ = MainClient.SqlClient.Alter(@"CREATE TABLE IF NOT EXISTS 'Blacklist' (
                'Id'	INTEGER,
                'IpAddress'	TEXT,
                'MacAddress'	TEXT,
                'Reason'	TEXT NOT NULL,
                'BlacklistedDate'	TEXT NOT NULL,
                PRIMARY KEY('Id' AUTOINCREMENT)
            )");

            // Create an index on Username for faster lookups
            _ = MainClient.SqlClient.Alter("CREATE INDEX IF NOT EXISTS idx_username ON Users(Username)");

            // Create indexes on IP and MAC addresses for blacklist checks
            _ = MainClient.SqlClient.Alter("CREATE INDEX IF NOT EXISTS idx_ip_address ON Users(IpAddress)");
            _ = MainClient.SqlClient.Alter("CREATE INDEX IF NOT EXISTS idx_mac_address ON Users(MacAddress)");
            _ = MainClient.SqlClient.Alter("CREATE INDEX IF NOT EXISTS idx_blacklist_ip ON Blacklist(IpAddress)");
            _ = MainClient.SqlClient.Alter("CREATE INDEX IF NOT EXISTS idx_blacklist_mac ON Blacklist(MacAddress)");
        }
    }
}
