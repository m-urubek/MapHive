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
                if (method.Name == "Run")
                    continue;
                int methodVersion = Int32.Parse(method.Name.Remove(0, 1));
                if (methodVersion > dbVersion)
                    method.Invoke(null, null);
                lastUpdateNumber = methodVersion;
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

        public static void v1()
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

        public static void v2()
        {
            // Create LogSeverity table to store different log levels
            MainClient.SqlClient.Alter(@"CREATE TABLE IF NOT EXISTS 'LogSeverity' (
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
                MainClient.SqlClient.Insert(
                    "INSERT INTO LogSeverity (Name, Description) VALUES (@Name, @Description);",
                    new SQLiteParameter[] { 
                        new SQLiteParameter("@Name", severityLevels[i]),
                        new SQLiteParameter("@Description", descriptions[i])
                    }
                );
            }

            // Create Logs table
            MainClient.SqlClient.Alter(@"CREATE TABLE IF NOT EXISTS 'Logs' (
                'Id'	INTEGER,
                'Timestamp' TEXT NOT NULL,
                'SeverityId' INTEGER NOT NULL,
                'Message' TEXT NOT NULL,
                'Source' TEXT,
                'Exception' TEXT,
                'StackTrace' TEXT,
                'UserName' TEXT,
                'RequestPath' TEXT,
                'AdditionalData' TEXT,
                PRIMARY KEY('Id' AUTOINCREMENT),
                FOREIGN KEY('SeverityId') REFERENCES 'LogSeverity'('Id')
            )");
        }
    }
}
