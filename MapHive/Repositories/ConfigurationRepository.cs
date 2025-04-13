using MapHive.Models;
using MapHive.Singletons;
using System.Data;
using System.Data.SQLite;

namespace MapHive.Repositories
{
    public class ConfigurationRepository : IConfigurationRepository
    {
        public ConfigurationItem? GetConfigurationItem(string key)
        {
            string query = "SELECT * FROM Configuration WHERE Key = @Key";
            SQLiteParameter[] parameters = new SQLiteParameter[] { new("@Key", key) };

            DataTable result = CurrentRequest.SqlClient.Select(query, parameters);

            return result.Rows.Count > 0 ? MapDataRowToConfigurationItem(result.Rows[0]) : null;
        }

        public List<ConfigurationItem> GetAllConfigurationItems()
        {
            string query = "SELECT * FROM Configuration";

            DataTable result = CurrentRequest.SqlClient.Select(query);
            List<ConfigurationItem> items = new();

            foreach (DataRow row in result.Rows)
            {
                items.Add(MapDataRowToConfigurationItem(row));
            }

            return items;
        }

        public int AddConfigurationItem(ConfigurationItem item)
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

            return CurrentRequest.SqlClient.Insert(query, parameters);
        }

        public int UpdateConfigurationItem(ConfigurationItem item)
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

            return CurrentRequest.SqlClient.Update(query, parameters);
        }

        public AppSettings GetAppSettings()
        {
            AppSettings settings = new();

            // Load all configuration items
            List<ConfigurationItem> items = this.GetAllConfigurationItems();

            // Map the configuration items to AppSettings properties
            foreach (ConfigurationItem item in items)
            {
                switch (item.Key)
                {
                    case "DevelopmentMode":
                        settings.DevelopmentMode = bool.TryParse(item.Value, out bool isDevelopmentMode) && isDevelopmentMode;
                        break;
                        // Add other configuration mappings as needed
                }
            }

            return settings;
        }

        private static ConfigurationItem MapDataRowToConfigurationItem(DataRow row)
        {
            return new ConfigurationItem
            {
                Id = Convert.ToInt32(row["Id_Configuration"]),
                Key = row["Key"].ToString() ?? string.Empty,
                Value = row["Value"].ToString() ?? string.Empty,
                Description = row["Description"] != DBNull.Value ? row["Description"].ToString() : null
            };
        }
    }
}