using MapHive.Models.RepositoryModels;
using MapHive.Singletons;
using System.Data;
using System.Data.SQLite;

namespace MapHive.Repositories
{
    public class ConfigurationRepository : IConfigurationRepository
    {
        private readonly ISqlClientSingleton _sqlClient;

        public ConfigurationRepository(ISqlClientSingleton sqlClient)
        {
            this._sqlClient = sqlClient;
        }

        public async Task<ConfigurationItem?> GetConfigurationItemAsync(string key)
        {
            string query = "SELECT * FROM Configuration WHERE Key = @Key";
            SQLiteParameter[] parameters = new SQLiteParameter[] { new("@Key", key) };

            DataTable result = await this._sqlClient.SelectAsync(query, parameters);

            return result.Rows.Count > 0 ? MapDataRowToConfigurationItem(result.Rows[0]) : null;
        }

        public async Task<List<ConfigurationItem>> GetAllConfigurationItemsAsync()
        {
            string query = "SELECT * FROM Configuration";

            DataTable result = await this._sqlClient.SelectAsync(query);
            List<ConfigurationItem> items = new();

            foreach (DataRow row in result.Rows)
            {
                items.Add(MapDataRowToConfigurationItem(row));
            }

            return items;
        }

        public async Task<int> AddConfigurationItemAsync(ConfigurationItem item)
        {
            string query = @"
                INSERT INTO Configuration (Key, Value, Description)
                VALUES (@Key, @Value, @Description)";

            SQLiteParameter[] parameters = new SQLiteParameter[]
            {
                new("@Key", item.Key),
                new("@Value", item.Value),
                new("@Description", item.Description as object ?? DBNull.Value)
            };

            return await this._sqlClient.InsertAsync(query, parameters);
        }

        public async Task<int> UpdateConfigurationItemAsync(ConfigurationItem item)
        {
            string query = @"
                UPDATE Configuration 
                SET Value = @Value, 
                    Description = @Description
                WHERE Key = @Key";

            SQLiteParameter[] parameters = new SQLiteParameter[]
            {
                new("@Value", item.Value),
                new("@Description", item.Description as object ?? DBNull.Value),
                new("@Key", item.Key)
            };

            return await this._sqlClient.UpdateAsync(query, parameters);
        }

        public async Task<string?> GetConfigurationValueAsync(string key)
        {
            ConfigurationItem? configItem = await this.GetConfigurationItemAsync(key);
            return configItem?.Value;
        }

        public async Task<int> DeleteConfigurationItemAsync(string key)
        {
            string query = "DELETE FROM Configuration WHERE Key = @Key";
            SQLiteParameter[] parameters = new SQLiteParameter[] { new("@Key", key) };
            return await this._sqlClient.DeleteAsync(query, parameters);
        }

        private static ConfigurationItem MapDataRowToConfigurationItem(DataRow row)
        {
            return new ConfigurationItem
            {
                Id = Convert.ToInt32(row["Id_Configuration"]),
                Key = row["Key"].ToString() ?? string.Empty,
                Value = row["Value"].ToString() ?? string.Empty,
                Description = row.Table.Columns.Contains("Description") && row["Description"] != DBNull.Value
                    ? row["Description"].ToString()
                    : null
            };
        }
    }
}