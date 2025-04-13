using MapHive.Models;
using MapHive.Repositories;
using System.Data.SQLite;

namespace MapHive.Singletons
{
    public static class MainClient
    {
        public static AppSettings? AppSettings { get; private set; }

        public static void Initialize()
        {
            DatabaseUpdater.Run();

            // Initialize AppSettings after database is updated
            InitializeAppSettings();
        }

        private static void InitializeAppSettings()
        {
            ConfigurationRepository configRepository = new();
            AppSettings = configRepository.GetAppSettings();
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
                        'Id_VersionNumber'    INTEGER,
                        'Value' INTEGER,
                        PRIMARY KEY('Id_VersionNumber' AUTOINCREMENT)
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
