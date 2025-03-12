using MapHive;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SuperSmashHoes
{
    public static class DatabaseUpdater
    {
        public static void Run()
        {
            // Check if VersionNumber table exists
            DataTable tableCheck = MainClient.SqlClient.Select(
                "SELECT name FROM sqlite_master WHERE type='table' AND name='VersionNumber'");
            if (tableCheck.Rows.Count == 0)
            {
                // Create the VersionNumber table
                CreateVersionNumberTable();
            }

            DataTable versionRawData = MainClient.SqlClient.Select("select * from VersionNumber");
            if (versionRawData.Rows.Count == 0)
                throw new Exception("Version data has no rows");
            int dbVersion;
            if (versionRawData.Rows[0]["Value"] == null || string.IsNullOrWhiteSpace(versionRawData.Rows[0]["Value"].ToString()))
                dbVersion = 0;
            else
                dbVersion = Int32.Parse(versionRawData.Rows[0]["Value"].ToString());
            MethodInfo[] versionMethods = typeof(DatabaseUpdater).GetMethods(BindingFlags.Public | BindingFlags.Static);
            int lastUpdateNumber = dbVersion;
            foreach (MethodInfo method in versionMethods)
            {
                if (method.Name == "Run" || method.Name == "CreateVersionNumberTable" || method.Name == "CreateMapLocationsTable")
                    continue;
                if (!method.Name.StartsWith("v"))
                    continue;
                int methodVersion = Int32.Parse(method.Name.Remove(0, 1));
                if (methodVersion > dbVersion)
                    method.Invoke(null, null);
                lastUpdateNumber = Math.Max(lastUpdateNumber, methodVersion);
            }
            if (lastUpdateNumber > dbVersion)
            {
                string query = "UPDATE VersionNumber SET Value = @Value WHERE Id = @Id";
                var parameters = new SQLiteParameter[]
                {
                    new SQLiteParameter("@Value", lastUpdateNumber),
                    new SQLiteParameter("@Id", versionRawData.Rows[0]["Id"])
                };
                if (MainClient.SqlClient.Update(query, parameters) != 1)
                    throw new Exception("Unable to update database number");
            }
        }

        private static void CreateVersionNumberTable()
        {
            MainClient.SqlClient.Alter(@"CREATE TABLE IF NOT EXISTS 'VersionNumber' (
	            'Id'	INTEGER,
	            'Value'	INTEGER,
	            PRIMARY KEY('Id' AUTOINCREMENT)
            )");
            
            MainClient.SqlClient.Insert(
                "INSERT INTO VersionNumber (Value) VALUES (@Value);",
                new SQLiteParameter[] { new SQLiteParameter("@Value", 0) }
            );
        }

        public static void v1()
        {
            CreateMapLocationsTable();
        }

        private static void CreateMapLocationsTable()
        {
            MainClient.SqlClient.Alter(@"CREATE TABLE IF NOT EXISTS 'MapLocations' (
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

            // Seed initial data
            var locations = new[]
            {
                new 
                {
                    Name = "Sample Location 1",
                    Description = "This is a sample location",
                    Latitude = 40.7128,
                    Longitude = -74.0060,
                    Address = "New York, NY, USA",
                    Website = "https://example.com",
                    PhoneNumber = "123-456-7890"
                },
                new 
                {
                    Name = "Sample Location 2",
                    Description = "This is another sample location",
                    Latitude = 34.0522,
                    Longitude = -118.2437,
                    Address = "Los Angeles, CA, USA",
                    Website = "https://example.org",
                    PhoneNumber = "987-654-3210"
                }
            };

            foreach (var location in locations)
            {
                var now = DateTime.UtcNow;
                MainClient.SqlClient.Insert(
                    @"INSERT INTO MapLocations (Name, Description, Latitude, Longitude, 
                    Address, Website, PhoneNumber, CreatedAt, UpdatedAt) 
                    VALUES (@Name, @Description, @Latitude, @Longitude, 
                    @Address, @Website, @PhoneNumber, @CreatedAt, @UpdatedAt)",
                    new SQLiteParameter[]
                    {
                        new SQLiteParameter("@Name", location.Name),
                        new SQLiteParameter("@Description", location.Description),
                        new SQLiteParameter("@Latitude", location.Latitude),
                        new SQLiteParameter("@Longitude", location.Longitude),
                        new SQLiteParameter("@Address", location.Address),
                        new SQLiteParameter("@Website", location.Website),
                        new SQLiteParameter("@PhoneNumber", location.PhoneNumber),
                        new SQLiteParameter("@CreatedAt", now),
                        new SQLiteParameter("@UpdatedAt", now)
                    }
                );
            }
        }
    }
}
