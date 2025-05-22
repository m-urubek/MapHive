namespace MapHive.Singletons;

using System.Data;
using System.Data.SQLite;
using System.Reflection;
using MapHive.Models.Enums;

/// <summary>
/// Service responsible for applying database schema updates.
/// </summary>
public class DatabaseUpdaterSingleton(ISqlClientSingleton sqlClientSingleton, ILogManagerSingleton logManagerSingleton, IFileLoggerSingleton fileLoggerSingleton) : IDatabaseUpdaterSingleton
{
    private readonly ISqlClientSingleton _sqlClientSingleton = sqlClientSingleton;
    private readonly ILogManagerSingleton _logManagerSingleton = logManagerSingleton;
    private readonly IFileLoggerSingleton _fileLoggerSingleton = fileLoggerSingleton;

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
                string query = "UPDATE VersionNumber SET Value = @Value WHERE Id_VersionNumber = @Id";
                SQLiteParameter[] parameters =
                [
                    new("@Value", lastUpdateNumber),
                    new("@Id", versionRawData.Rows[0]["Id_VersionNumber"])
                ];
                _ = await _sqlClientSingleton.UpdateOrThrowAsync(query: query, parameters: parameters);
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
    [System.Diagnostics.CodeAnalysis.SuppressMessage(category: "Style", checkId: "IDE0051:Remove unused private members")]
    private async Task V1()
    {
        _fileLoggerSingleton.LogToFile(message: "Database Updater V1: Starting initial data setup.");
        Console.WriteLine("Database Updater V1: Starting initial data setup.");

        // Insert initial DevelopmentMode configuration
        _ = await _sqlClientSingleton.InsertAsync(query: "INSERT INTO Configuration (Key, Value, Description) VALUES ('DevelopmentMode', '1', 'Set to 1 to enable development mode.');", parameters: null);
        _fileLoggerSingleton.LogToFile(message: "Database Updater V1: Development mode (1) inserted.");
        Console.WriteLine("Database Updater V1: Development mode (1) inserted.");

        // Add initial admin user
        string adminUsername = "admin";
        string adminPassword = "admin"; // Plain text password
        string hashedPassword = MapHive.Utilities.HashingUtility.HashPassword(password: adminPassword);
        DateTime registrationDate = DateTime.UtcNow;
        int adminTier = (int)AccountTier.Admin;
        string insertAdminQuery = @"
                INSERT into Accounts (Username, PasswordHash, RegistrationDate, Tier, IpAddressHistory)
                VALUES (@Username, @PasswordHash, @RegistrationDate, @Tier, @IpAddressHistory);
            ";
        SQLiteParameter[] adminParams =
        [
            new SQLiteParameter("@Username", adminUsername),
            new SQLiteParameter("@PasswordHash", hashedPassword),
            new SQLiteParameter("@RegistrationDate", registrationDate.ToString("o")),
            new SQLiteParameter("@Tier", adminTier),
            new SQLiteParameter("@IpAddressHistory", "INITIAL_SETUP")
        ];
        int adminId = await _sqlClientSingleton.InsertAsync(query: insertAdminQuery, parameters: adminParams);
        _fileLoggerSingleton.LogToFile(message: $"Database Updater V1: Initial admin user 'admin' created with ID: {adminId}.");
        Console.WriteLine($"Database Updater V1: Initial admin user 'admin' created with ID: {adminId}.");

        // Insert sample categories
        string[] categoryNames = ["General", "Historical", "Natural Wonder"];
        string[] categoryDescriptions = ["General purpose locations", "Places of historical significance", "Natural formations and points of interest"];
        List<int> categoryIds = [];

        for (int i = 0; i < categoryNames.Length; i++)
        {
            string insertCategoryQuery = "INSERT INTO Categories (Name, Description) VALUES (@Name, @Description);";
            SQLiteParameter[] categoryParams =
            [
                new SQLiteParameter("@Name", categoryNames[i]),
                new SQLiteParameter("@Description", categoryDescriptions[i])
            ];
            int categoryId = await _sqlClientSingleton.InsertAsync(query: insertCategoryQuery, parameters: categoryParams);
            categoryIds.Add(categoryId);
            _fileLoggerSingleton.LogToFile(message: $"Database Updater V1: Category '{categoryNames[i]}' inserted with ID: {categoryId}.");
            Console.WriteLine($"Database Updater V1: Category '{categoryNames[i]}' inserted with ID: {categoryId}.");
        }

        if (categoryIds.Count == 0)
        {
            _fileLoggerSingleton.LogToFile(message: "Database Updater V1: No categories were created. Skipping map location insertion.");
            Console.WriteLine("Database Updater V1: No categories were created. Skipping map location insertion.");
            return;
        }
        int sampleCategoryId = categoryIds[0]; // Use the first category for all sample locations for simplicity

        // Insert sample map locations
        var locationsToInsert = new[]
        {
            new { Title = "Eiffel Tower", Description = "Iconic tower in Paris, France.", Latitude = 48.8584, Longitude = 2.2945 },
            new { Title = "Colosseum", Description = "Ancient amphitheater in Rome, Italy.", Latitude = 41.8902, Longitude = 12.4922 },
            new { Title = "Grand Canyon", Description = "Massive canyon in Arizona, USA.", Latitude = 36.1070, Longitude = -112.1130 }
        };

        foreach (var loc in locationsToInsert)
        {
            string insertLocationQuery = @"
                    INSERT INTO MapLocations (Name, Description, Latitude, Longitude, Address, Website, PhoneNumber, CategoryId, OwnerId, CreatedAt, UpdatedAt, IsAnonymous)
                    VALUES (@Name, @Description, @Latitude, @Longitude, @Address, @Website, @PhoneNumber, @CategoryId, @OwnerId, @CreatedAt, @UpdatedAt, @IsAnonymous);
                ";
            SQLiteParameter[] locationParams =
            [
                new SQLiteParameter("@Name", loc.Title),
                new SQLiteParameter("@Description", loc.Description),
                new SQLiteParameter("@Latitude", loc.Latitude),
                new SQLiteParameter("@Longitude", loc.Longitude),
                new SQLiteParameter("@Address", DBNull.Value),
                new SQLiteParameter("@Website", DBNull.Value),
                new SQLiteParameter("@PhoneNumber", DBNull.Value),
                new SQLiteParameter("@CategoryId", sampleCategoryId),
                new SQLiteParameter("@OwnerId", adminId),
                new SQLiteParameter("@CreatedAt", DateTime.UtcNow.ToString("o")),
                new SQLiteParameter("@UpdatedAt", DateTime.UtcNow.ToString("o")),
                new SQLiteParameter("@IsAnonymous", false)
            ];
            int locationId = await _sqlClientSingleton.InsertAsync(query: insertLocationQuery, parameters: locationParams);
            _fileLoggerSingleton.LogToFile(message: $"Database Updater V1: Map location '{loc.Title}' inserted with ID: {locationId}.");
            Console.WriteLine($"Database Updater V1: Map location '{loc.Title}' inserted with ID: {locationId}.");
        }

        _fileLoggerSingleton.LogToFile(message: "Database Updater V1: Finished initial data setup.");
        Console.WriteLine("Database Updater V1: Finished initial data setup.");
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(category: "Style", checkId: "IDE0051:Remove unused private members")]
    private async Task V2()
    {
        _fileLoggerSingleton.LogToFile(message: "Database Updater V2: Adding DarkModeEnabled column to Accounts.");
        Console.WriteLine("Database Updater V2: Adding DarkModeEnabled column to Accounts.");
        _ = await _sqlClientSingleton.AlterAsync("ALTER TABLE Accounts ADD COLUMN DarkModeEnabled INTEGER NOT NULL DEFAULT 1;");
    }
}
