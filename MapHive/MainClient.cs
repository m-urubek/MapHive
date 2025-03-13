using MapHive.Services;
using SuperSmashHoes;
using System.Data.SQLite;

namespace MapHive
{
    public static class MainClient
    {
        public static SqlClient SqlClient { get; private set; }
        public static LogManager LogManager { get; private set; }

        public static void Initialize()
        {
            string dbFilePath = "D:\\MapHive\\MapHive\\maphive.db";
            if (!File.Exists(dbFilePath))
                dbFilePath = "maphive.db";
            if (!File.Exists(dbFilePath))
                throw new Exception("Database not found!");

            MainClient.SqlClient = new(dbFilePath);

            DatabaseUpdater.Run();
        }
    }
}
