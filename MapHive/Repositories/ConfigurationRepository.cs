namespace MapHive.Repositories
{
    using System.Data;
    using System.Data.SQLite;
    using MapHive.Models.RepositoryModels;
    using MapHive.Services;
    using MapHive.Singletons;
    using MapHive.Utilities;

    public class ConfigurationRepository(ISqlClientSingleton sqlClientSingleton, ILogManagerService logManagerService) : IConfigurationRepository
    {
        private readonly ISqlClientSingleton _sqlClientSingleton = sqlClientSingleton;
        private readonly ILogManagerService _logManagerService = logManagerService;

        public async Task<ConfigurationItem?> GetConfigurationItemAsync(string key)
        {
            string query = "SELECT * FROM Configuration WHERE Key = @Key";
            SQLiteParameter[] parameters = [new("@Key", key)];

            DataTable result = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);

            return result.Rows.Count > 0 ? MapDataRowToConfigurationItem(row: result.Rows[0]) : null;
        }

        public async Task<List<ConfigurationItem>> GetAllConfigurationItemsAsync()
        {
            string query = "SELECT * FROM Configuration";

            DataTable result = await _sqlClientSingleton.SelectAsync(query: query);
            List<ConfigurationItem> items = new();

            foreach (DataRow row in result.Rows)
            {
                items.Add(item: MapDataRowToConfigurationItem(row: row));
            }

            return items;
        }

        public async Task<int> AddConfigurationItemAsync(ConfigurationItem item)
        {
            string query = @"
                INSERT INTO Configuration (Key, Value, Description)
                VALUES (@Key, @Value, @Description)";

            SQLiteParameter[] parameters =
            [
                new("@Key", item.Key),
                new("@Value", item.Value),
                new("@Description", item.Description as object ?? DBNull.Value)
            ];

            return await _sqlClientSingleton.InsertAsync(query: query, parameters: parameters);
        }

        public async Task<int> UpdateConfigurationItemAsync(ConfigurationItem item)
        {
            string query = @"
                UPDATE Configuration 
                SET Value = @Value, 
                    Description = @Description
                WHERE Key = @Key";

            SQLiteParameter[] parameters =
            [
                new("@Value", item.Value),
                new("@Description", item.Description as object ?? DBNull.Value),
                new("@Key", item.Key)
            ];

            return await _sqlClientSingleton.UpdateAsync(query: query, parameters: parameters);
        }

        public async Task<string?> GetConfigurationValueAsync(string key)
        {
            ConfigurationItem? configItem = await GetConfigurationItemAsync(key: key);
            return configItem?.Value;
        }

        public async Task<int> DeleteConfigurationItemAsync(string key)
        {
            string query = "DELETE FROM Configuration WHERE Key = @Key";
            SQLiteParameter[] parameters = [new("@Key", key)];
            return await _sqlClientSingleton.DeleteAsync(query: query, parameters: parameters);
        }

        private ConfigurationItem MapDataRowToConfigurationItem(DataRow row)
        {
            const string table = "Configuration";
            return new ConfigurationItem
            {
                Id = row.GetValueOrDefault(_logManagerService, tableName: table, columnName: "Id_Configuration", isRequired: true, converter: Convert.ToInt32),
                Key = row.GetValueOrDefault(_logManagerService, tableName: table, columnName: "Key", isRequired: true, converter: v => v.ToString()!, defaultValue: string.Empty),
                Value = row.GetValueOrDefault(_logManagerService, tableName: table, columnName: "Value", isRequired: true, converter: v => v.ToString()!, defaultValue: string.Empty),
                Description = row.GetValueOrDefault(_logManagerService, tableName: table, columnName: "Description", isRequired: false, converter: v => v.ToString()!, defaultValue: default)
            };
        }
    }
}