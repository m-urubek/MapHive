using MapHive.Models;
using MapHive.Repositories;
using MapHive.Singletons;
using System.Data.SQLite;

namespace MapHive.Services
{
    public class ConfigService : IConfigService
    {
        private readonly IConfigurationRepository _configRepository;

        public ConfigService(IConfigurationRepository configRepository)
        {
            this._configRepository = configRepository;
        }

        public async Task<ConfigurationItem?> GetConfigurationItemAsync(string key)
        {
            return await Task.Run(() => this._configRepository.GetConfigurationItem(key));
        }

        public async Task<List<ConfigurationItem>> GetAllConfigurationItemsAsync()
        {
            return await Task.Run(this._configRepository.GetAllConfigurationItems);
        }

        public async Task<int> AddConfigurationItemAsync(ConfigurationItem item)
        {
            return await Task.Run(() => this._configRepository.AddConfigurationItem(item));
        }

        public async Task<int> UpdateConfigurationItemAsync(ConfigurationItem item)
        {
            return await Task.Run(() => this._configRepository.UpdateConfigurationItem(item));
        }

        public async Task<bool> DeleteConfigurationItemAsync(string key)
        {
            return await Task.Run(() =>
            {
                string query = "DELETE FROM Configuration WHERE Key = @Key";
                SQLiteParameter[] parameters = new SQLiteParameter[]
                {
                    new("@Key", key)
                };

                int rowsAffected = CurrentRequest.SqlClient.Delete(query, parameters);
                return rowsAffected > 0;
            });
        }
    }
}