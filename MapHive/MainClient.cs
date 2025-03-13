using MapHive.Services;
using SuperSmashHoes;
using System.Data.SQLite;

namespace MapHive
{
    public static class MainClient
    {
        public static SqlClient SqlClient { get; private set; }
        public static LogManager? LogManager { get; private set; }

        public static void Initialize()
        {
            string dbFilePath = "D:\\MapHive\\MapHive\\maphive.db";
            if (!File.Exists(dbFilePath))
            {
                dbFilePath = "maphive.db";
            }

            if (!File.Exists(dbFilePath))
            {
                // Create new database file with version 0 instead of throwing an exception
                CreateNewDatabase(dbFilePath);
            }

            MainClient.SqlClient = new(dbFilePath);

            DatabaseUpdater.Run();
        }

        private static void CreateNewDatabase(string dbFilePath)
        {
            // Create the SQLite database file
            SQLiteConnection.CreateFile(dbFilePath);

            // Create a connection to the new database
            using SQLiteConnection connection = new($"Data Source={dbFilePath};Version=3;");
            connection.Open();

            // Create the VersionNumber table
            using (SQLiteCommand command = new(
                @"CREATE TABLE IF NOT EXISTS 'VersionNumber' (
                        'Id'    INTEGER,
                        'Value' INTEGER,
                        PRIMARY KEY('Id' AUTOINCREMENT)
                    )", connection))
            {
                _ = command.ExecuteNonQuery();
            }

            // Insert version 0
            using (SQLiteCommand command = new(
                "INSERT INTO VersionNumber (Value) VALUES (0)", connection))
            {
                _ = command.ExecuteNonQuery();
            }
        }
    }
}
