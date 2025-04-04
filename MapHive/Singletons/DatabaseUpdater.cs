﻿using System.Data;
using System.Data.SQLite;
using System.Reflection;

namespace MapHive.Singletons
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

                int methodVersion = int.Parse(method.Name[1..]);
                if (methodVersion > dbVersion)
                {
                    _ = method.Invoke(null, null);
                }

                lastUpdateNumber = methodVersion;
            }
            if (lastUpdateNumber > dbVersion)
            {
                string query = "UPDATE VersionNumber SET Value = @Value WHERE Id_VersionNumber = @Id";
                SQLiteParameter[] parameters = new SQLiteParameter[]
                {
                    new("@Value", lastUpdateNumber),
                    new("@Id", versionRawData.Rows[0]["Id_VersionNumber"])
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
                'Id_MapLocation'	INTEGER,
                'Name'	TEXT NOT NULL,
                'Description'	TEXT NOT NULL,
                'Latitude'	REAL NOT NULL,
                'Longitude'	REAL NOT NULL,
                'Address'	TEXT,
                'Website'	TEXT,
                'PhoneNumber'	TEXT,
                'CreatedAt'	TEXT NOT NULL,
                'UpdatedAt'	TEXT NOT NULL,
                PRIMARY KEY('Id_MapLocation' AUTOINCREMENT)
            )");
        }

        public static void v2()
        {
            // Create LogSeverity table to store different log levels
            _ = MainClient.SqlClient.Alter(@"CREATE TABLE IF NOT EXISTS 'LogSeverity' (
                'Id_LogSeverity'	INTEGER,
                'Name'	TEXT NOT NULL,
                'Description' TEXT,
                PRIMARY KEY('Id_LogSeverity' AUTOINCREMENT)
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
                'Id_Log'	INTEGER,
                'Timestamp' TEXT NOT NULL,
                'SeverityId' INTEGER NOT NULL,
                'Message' TEXT NOT NULL,
                'Source' TEXT,
                'Exception' TEXT,
                'UserName' TEXT,
                'RequestPath' TEXT,
                'AdditionalData' TEXT,
                PRIMARY KEY('Id_Log' AUTOINCREMENT),
                FOREIGN KEY('SeverityId') REFERENCES 'LogSeverity'('Id_LogSeverity')
            )");
        }

        public static void v3()
        {
            // Create Users table
            _ = MainClient.SqlClient.Alter(@"CREATE TABLE IF NOT EXISTS 'Users' (
                'Id_User'	INTEGER,
                'Username'	TEXT NOT NULL UNIQUE,
                'PasswordHash'	TEXT NOT NULL,
                'RegistrationDate'	TEXT NOT NULL,
                'IpAddress'	TEXT NOT NULL,
                'MacAddress'	TEXT NOT NULL,
                'IsTrusted'	INTEGER NOT NULL DEFAULT 0,
                'IsAdmin'	INTEGER NOT NULL DEFAULT 0,
                PRIMARY KEY('Id_User' AUTOINCREMENT)
            )");

            // Create blacklist table for IP and MAC addresses
            _ = MainClient.SqlClient.Alter(@"CREATE TABLE IF NOT EXISTS 'Blacklist' (
                'Id_Blacklist'	INTEGER,
                'IpAddress'	TEXT,
                'MacAddress'	TEXT,
                'Reason'	TEXT NOT NULL,
                'BlacklistedDate'	TEXT NOT NULL,
                PRIMARY KEY('Id_Blacklist' AUTOINCREMENT)
            )");

            // Create an index on Username for faster lookups
            _ = MainClient.SqlClient.Alter("CREATE INDEX IF NOT EXISTS idx_username ON Users(Username)");

            // Create indexes on IP and MAC addresses for blacklist checks
            _ = MainClient.SqlClient.Alter("CREATE INDEX IF NOT EXISTS idx_ip_address ON Users(IpAddress)");
            _ = MainClient.SqlClient.Alter("CREATE INDEX IF NOT EXISTS idx_mac_address ON Users(MacAddress)");
            _ = MainClient.SqlClient.Alter("CREATE INDEX IF NOT EXISTS idx_blacklist_ip ON Blacklist(IpAddress)");
            _ = MainClient.SqlClient.Alter("CREATE INDEX IF NOT EXISTS idx_blacklist_mac ON Blacklist(MacAddress)");
        }

        public static void v4()
        {
            // Create Configuration table
            _ = MainClient.SqlClient.Alter(@"CREATE TABLE IF NOT EXISTS 'Configuration' (
                'Id_Configuration'	INTEGER,
                'Key'	TEXT NOT NULL UNIQUE,
                'Value'	TEXT NOT NULL,
                'Description'	TEXT,
                PRIMARY KEY('Id_Configuration' AUTOINCREMENT)
            )");

            // Create an index on the Key column for faster lookups
            _ = MainClient.SqlClient.Alter("CREATE INDEX IF NOT EXISTS idx_configuration_key ON Configuration(Key)");

            // Add DevelopmentMode configuration with default value of false
            string query = @"
                INSERT INTO Configuration (Key, Value, Description)
                VALUES (@Key, @Value, @Description)";

            SQLiteParameter[] parameters = new SQLiteParameter[]
            {
                new("@Key", "DevelopmentMode"),
                new("@Value", "false"),
                new("@Description", "Enable development features and debugging tools when true")
            };

            _ = MainClient.SqlClient.Insert(query, parameters);
        }

        public static void v5()
        {
            // Path to the migration script
            string sqlUpdateScriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "SqlUpdates", "AddUserIdToMapLocations.sql");

            // Check if the migration script exists
            if (!File.Exists(sqlUpdateScriptPath))
            {
                throw new FileNotFoundException("Could not find the migration script.", sqlUpdateScriptPath);
            }

            // Read the migration script
            string migrationScript = File.ReadAllText(sqlUpdateScriptPath);

            // Execute the migration script
            int statementsExecuted = MainClient.SqlClient.ExecuteScript(migrationScript);

            // Log the migration result
            Console.WriteLine($"Executed {statementsExecuted} statements from MapLocations migration script.");
        }

        public static void v6()
        {
            string sqlUpdateScriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "SqlUpdates", "ReviewsAndDiscussions.sql");

            // Check if the migration script exists
            if (!File.Exists(sqlUpdateScriptPath))
            {
                throw new FileNotFoundException("Could not find the migration script.", sqlUpdateScriptPath);
            }

            // Read the migration script
            string migrationScript = File.ReadAllText(sqlUpdateScriptPath);

            // Execute the migration script
            int statementsExecuted = MainClient.SqlClient.ExecuteScript(migrationScript);

            // Log the migration result
            Console.WriteLine($"Executed {statementsExecuted} statements from ReviewsAndDiscussions sql update script.");
        }

        public static void v7()
        {
            // Remove MAC address functionality

            // Create temporary tables without MAC address
            _ = MainClient.SqlClient.Alter(@"CREATE TABLE IF NOT EXISTS 'Users_temp' (
                'Id_User'	INTEGER,
                'Username'	TEXT NOT NULL UNIQUE,
                'PasswordHash'	TEXT NOT NULL,
                'RegistrationDate'	TEXT NOT NULL,
                'IpAddress'	TEXT NOT NULL,
                'IsTrusted'	INTEGER NOT NULL DEFAULT 0,
                'IsAdmin'	INTEGER NOT NULL DEFAULT 0,
                PRIMARY KEY('Id_User' AUTOINCREMENT)
            )");

            _ = MainClient.SqlClient.Alter(@"CREATE TABLE IF NOT EXISTS 'Blacklist_temp' (
                'Id_Blacklist'	INTEGER,
                'IpAddress'	TEXT,
                'Reason'	TEXT NOT NULL,
                'BlacklistedDate'	TEXT NOT NULL,
                PRIMARY KEY('Id_Blacklist' AUTOINCREMENT)
            )");

            // Copy data to temporary tables without MAC address
            _ = MainClient.SqlClient.Alter(@"INSERT INTO Users_temp (Id_User, Username, PasswordHash, RegistrationDate, IpAddress, IsTrusted, IsAdmin)
                SELECT Id_User, Username, PasswordHash, RegistrationDate, IpAddress, IsTrusted, IsAdmin FROM Users");

            _ = MainClient.SqlClient.Alter(@"INSERT INTO Blacklist_temp (Id_Blacklist, IpAddress, Reason, BlacklistedDate)
                SELECT Id_Blacklist, IpAddress, Reason, BlacklistedDate FROM Blacklist");

            // Drop original tables
            _ = MainClient.SqlClient.Alter("DROP TABLE Users");
            _ = MainClient.SqlClient.Alter("DROP TABLE Blacklist");

            // Rename temporary tables to original names
            _ = MainClient.SqlClient.Alter("ALTER TABLE Users_temp RENAME TO Users");
            _ = MainClient.SqlClient.Alter("ALTER TABLE Blacklist_temp RENAME TO Blacklist");

            // Recreate indexes
            _ = MainClient.SqlClient.Alter("CREATE INDEX IF NOT EXISTS idx_username ON Users(Username)");
            _ = MainClient.SqlClient.Alter("CREATE INDEX IF NOT EXISTS idx_ip_address ON Users(IpAddress)");
            _ = MainClient.SqlClient.Alter("CREATE INDEX IF NOT EXISTS idx_blacklist_ip ON Blacklist(IpAddress)");
        }

        public static void v8()
        {
            // Step 1: Add the Tier column if it doesn't exist
            _ = MainClient.SqlClient.Alter(@"
                PRAGMA foreign_keys=off;

                BEGIN TRANSACTION;

                -- Add Tier column to Users table
                ALTER TABLE Users ADD COLUMN Tier INTEGER DEFAULT 0;

                -- Update Tier values based on existing boolean flags
                UPDATE Users SET Tier = 
                    CASE 
                        WHEN IsAdmin = 1 THEN 2
                        WHEN IsTrusted = 1 THEN 1
                        ELSE 0
                    END;

                -- Create a temporary table without the old columns
                CREATE TABLE Users_temp (
                    Id_User INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT NOT NULL UNIQUE,
                    PasswordHash TEXT NOT NULL,
                    RegistrationDate TEXT NOT NULL,
                    IpAddress TEXT NOT NULL,
                    Tier INTEGER NOT NULL DEFAULT 0
                );

                -- Copy data to the new table structure
                INSERT INTO Users_temp (Id_User, Username, PasswordHash, RegistrationDate, IpAddress, Tier)
                SELECT Id_User, Username, PasswordHash, RegistrationDate, IpAddress, Tier FROM Users;

                -- Drop the old table and rename the new one
                DROP TABLE Users;
                ALTER TABLE Users_temp RENAME TO Users;

                -- Recreate indexes
                CREATE INDEX idx_username ON Users(Username);
                CREATE INDEX idx_ip_address ON Users(IpAddress);

                COMMIT;

                PRAGMA foreign_keys=on;
            ");

        }

        public static void v9()
        {
            _ = MainClient.SqlClient.Alter(@"
                PRAGMA foreign_keys=off;
                
                BEGIN TRANSACTION;
                
                -- Create UserBans table for storing user and IP bans
                CREATE TABLE IF NOT EXISTS 'UserBans' (
                    'Id_UserBan' INTEGER PRIMARY KEY AUTOINCREMENT,
                    'UserId' INTEGER,
                    'IpAddress' TEXT,
                    'BanType' INTEGER NOT NULL,
                    'BannedAt' TEXT NOT NULL,
                    'ExpiresAt' TEXT,
                    'Reason' TEXT NOT NULL,
                    'BannedByUserId' INTEGER NOT NULL,
                    FOREIGN KEY('UserId') REFERENCES 'Users'('Id_User') ON DELETE CASCADE,
                    FOREIGN KEY('BannedByUserId') REFERENCES 'Users'('Id_User') ON DELETE CASCADE
                );
                
                -- Create indexes for faster lookups
                CREATE INDEX IF NOT EXISTS idx_userbans_userid ON UserBans(UserId);
                CREATE INDEX IF NOT EXISTS idx_userbans_ipaddress ON UserBans(IpAddress);
                CREATE INDEX IF NOT EXISTS idx_userbans_expirydate ON UserBans(ExpiresAt);
                CREATE INDEX IF NOT EXISTS idx_userbans_bannedby ON UserBans(BannedByUserId);
                
                COMMIT;
                
                PRAGMA foreign_keys=on;
            ");
        }

        public static void v10()
        {
            _ = MainClient.SqlClient.ExecuteScript(@"
                PRAGMA foreign_keys=off;
                
                BEGIN TRANSACTION;
                
                -- Step 1: Add the new IpAddressHistory column
                ALTER TABLE Users ADD COLUMN IpAddressHistory TEXT DEFAULT '';
                
                -- Step 2: Copy the existing registration IP to the new history column
                -- Only update if IpAddressHistory is currently the default empty string
                UPDATE Users SET IpAddressHistory = IpAddress WHERE IpAddressHistory = '';
                
                -- Step 3: Create a temporary table with the final schema (without IpAddress)
                CREATE TABLE Users_temp (
                    Id_User INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT NOT NULL UNIQUE,
                    PasswordHash TEXT NOT NULL,
                    RegistrationDate TEXT NOT NULL,
                    Tier INTEGER NOT NULL DEFAULT 0,
                    IpAddressHistory TEXT DEFAULT '' -- Keep the new column
                );
                
                -- Step 4: Copy data from the old table to the temporary table
                INSERT INTO Users_temp (Id_User, Username, PasswordHash, RegistrationDate, Tier, IpAddressHistory)
                SELECT Id_User, Username, PasswordHash, RegistrationDate, Tier, IpAddressHistory FROM Users;
                
                -- Step 5: Drop the old Users table
                DROP TABLE Users;
                
                -- Step 6: Rename the temporary table to Users
                ALTER TABLE Users_temp RENAME TO Users;
                
                -- Step 7: Recreate necessary indexes (excluding the one for the dropped IpAddress column)
                CREATE INDEX IF NOT EXISTS idx_username ON Users(Username);
                -- Do NOT recreate idx_ip_address
                
                COMMIT;
                
                PRAGMA foreign_keys=on;
            ");
        }
    }
}