namespace MapHive.Singletons
{
    using System.Data;
    using System.Data.SQLite;
    using System.Reflection;
    using MapHive.Models.Enums;

    /// <summary>
    /// Service responsible for applying database schema updates.
    /// </summary>
    public class DatabaseUpdaterSingleton(ISqlClientSingleton sqlClientSingleton, ILogManagerSingleton logManagerSingleton) : IDatabaseUpdaterSingleton
    {
        private readonly ISqlClientSingleton _sqlClientSingleton = sqlClientSingleton;
        private readonly ILogManagerSingleton _logManagerSingleton = logManagerSingleton;

        /// <summary>
        /// Checks the current database version and applies necessary updates.
        /// </summary>
        public async Task RunAsync()
        {
            try
            {
                DataTable versionRawData = await _sqlClientSingleton.SelectAsync(query: "SELECT * FROM VersionNumber ORDER BY Id_VersionNumber LIMIT 1");
                if (versionRawData.Rows.Count == 0)
                {
                    _ = _logManagerSingleton.LogAsync(severity: LogSeverity.Error, message: "Database Updater: VersionNumber table is empty or does not exist.");
                    throw new Exception("Database Updater: VersionNumber table is empty or does not exist.");
                }

                int dbVersion = versionRawData.Rows[0]["Value"] == DBNull.Value || string.IsNullOrWhiteSpace(value: versionRawData.Rows[0]["Value"].ToString())
                    ? 0
                    : int.Parse(s: versionRawData.Rows[0]["Value"].ToString()!);

                MethodInfo[] versionMethods = [.. typeof(DatabaseUpdaterSingleton).GetMethods(bindingAttr: BindingFlags.NonPublic | BindingFlags.Instance)
                                                  .Where(predicate: m => m.Name.StartsWith(value: 'V') && m.Name.Length > 1 && int.TryParse(s: m.Name[1..], result: out _))
                                                  .OrderBy(keySelector: m => int.Parse(s: m.Name[1..]))];

                int lastUpdateNumber = dbVersion;
                bool updatesApplied = false;

                foreach (MethodInfo method in versionMethods)
                {
                    int methodVersion = int.Parse(s: method.Name[1..]);
                    if (methodVersion > dbVersion)
                    {
                        _ = _logManagerSingleton.LogAsync(severity: LogSeverity.Information, message: $"Database Updater: Applying update V{methodVersion}...");
                        Task? task = (Task?)method.Invoke(obj: this, parameters: null);
                        if (task != null)
                        {
                            await task;
                            _ = _logManagerSingleton.LogAsync(severity: LogSeverity.Information, message: $"Database Updater: Update V{methodVersion} applied successfully.");
                            lastUpdateNumber = methodVersion;
                            updatesApplied = true;
                        }
                        else
                        {
                            _ = _logManagerSingleton.LogAsync(severity: LogSeverity.Warning, message: $"Database Updater: Update method V{methodVersion} did not return a Task.");
                        }
                    }
                }

                if (updatesApplied)
                {
                    string query = "UPDATE VersionNumber SET Value = @Value WHERE Id_VersionNumber = @Id_Log";
                    SQLiteParameter[] parameters =
                    [
                        new("@Value", lastUpdateNumber),
                        new("@Id_Log", versionRawData.Rows[0]["Id_VersionNumber"])
                    ];
                    if (await _sqlClientSingleton.UpdateAsync(query: query, parameters: parameters) != 1)
                    {
                        throw new Exception($"Database Updater: Failed to update database version number to {lastUpdateNumber}.");
                    }
                    _ = _logManagerSingleton.LogAsync(severity: LogSeverity.Information, message: $"Database Updater: Database version updated to {lastUpdateNumber}.");
                }
                else
                {
                    _ = _logManagerSingleton.LogAsync(severity: LogSeverity.Information, message: "Database Updater: Database is up to date.");
                }
            }
            catch (Exception ex)
            {
                _ = _logManagerSingleton.LogAsync(severity: LogSeverity.Critical, message: "Database Updater: An error occurred during database update.", exception: ex, source: nameof(DatabaseUpdaterSingleton));
                // Rethrow or handle critical failure appropriately
                throw;
            }
        }

        // --- Update Methods ---
        // Add private async Task methods named V1, V2, etc. for each database update.
    }
}
