using MapHive.Models.Enums;
using MapHive.Services;
using System.Data;
using System.Data.SQLite;
using System.Reflection;

namespace MapHive.Singletons
{
    /// <summary>
    /// Service responsible for applying database schema updates.
    /// </summary>
    public class DatabaseUpdaterService : IDatabaseUpdaterSingleton
    {
        private readonly ISqlClientSingleton _sqlClientSingleton;
        private readonly ILogManagerSingleton _logManagerSingleton;

        public DatabaseUpdaterService(ISqlClientSingleton sqlClientSingleton, ILogManagerSingleton logManagerSingleton)
        {
            this._sqlClientSingleton = sqlClientSingleton;
            this._logManagerSingleton = logManagerSingleton;
        }

        /// <summary>
        /// Checks the current database version and applies necessary updates.
        /// </summary>
        public async Task RunAsync()
        {
            try
            {
                DataTable versionRawData = await this._sqlClientSingleton.SelectAsync("SELECT * FROM VersionNumber ORDER BY Id_VersionNumber LIMIT 1");
                if (versionRawData.Rows.Count == 0)
                {
                    this._logManagerSingleton.Log(LogSeverity.Error, "Database Updater: VersionNumber table is empty or does not exist.");
                    // Consider throwing a specific exception or handling initialization differently
                    throw new Exception("Database Updater: VersionNumber table is empty or does not exist.");
                }

                int dbVersion = versionRawData.Rows[0]["Value"] == DBNull.Value || string.IsNullOrWhiteSpace(versionRawData.Rows[0]["Value"].ToString())
                    ? 0
                    : int.Parse(versionRawData.Rows[0]["Value"].ToString()!);

                MethodInfo[] versionMethods = typeof(DatabaseUpdaterService).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                                                  .Where(m => m.Name.StartsWith("V") && m.Name.Length > 1 && int.TryParse(m.Name[1..], out _))
                                                  .OrderBy(m => int.Parse(m.Name[1..]))
                                                  .ToArray();

                int lastUpdateNumber = dbVersion;
                bool updatesApplied = false;

                foreach (MethodInfo method in versionMethods)
                {
                    int methodVersion = int.Parse(method.Name[1..]);
                    if (methodVersion > dbVersion)
                    {
                        this._logManagerSingleton.Log(LogSeverity.Information, $"Database Updater: Applying update V{methodVersion}...");
                        Task? task = (Task?)method.Invoke(this, null);
                        if (task != null)
                        {
                            await task;
                            this._logManagerSingleton.Log(LogSeverity.Information, $"Database Updater: Update V{methodVersion} applied successfully.");
                            lastUpdateNumber = methodVersion;
                            updatesApplied = true;
                        }
                        else
                        {
                            this._logManagerSingleton.Log(LogSeverity.Warning, $"Database Updater: Update method V{methodVersion} did not return a Task.");
                        }
                    }
                }

                if (updatesApplied)
                {
                    string query = "UPDATE VersionNumber SET Value = @Value WHERE Id_VersionNumber = @Id_Log";
                    SQLiteParameter[] parameters = new SQLiteParameter[]
                    {
                        new("@Value", lastUpdateNumber),
                        new("@Id_Log", versionRawData.Rows[0]["Id_VersionNumber"])
                    };
                    if (await this._sqlClientSingleton.UpdateAsync(query, parameters) != 1)
                    {
                        throw new Exception($"Database Updater: Failed to update database version number to {lastUpdateNumber}.");
                    }
                    this._logManagerSingleton.Log(LogSeverity.Information, $"Database Updater: Database version updated to {lastUpdateNumber}.");
                }
                else
                {
                    this._logManagerSingleton.Log(LogSeverity.Information, "Database Updater: Database is up to date.");
                }
            }
            catch (Exception ex)
            {
                this._logManagerSingleton.Log(LogSeverity.Critical, "Database Updater: An error occurred during database update.", ex, nameof(DatabaseUpdaterService));
                // Rethrow or handle critical failure appropriately
                throw;
            }
        }

        // --- Update Methods ---
        // Add private async Task methods named V1, V2, etc. for each database update.
    }
}