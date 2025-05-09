using MapHive.Models.RepositoryModels;
using MapHive.Repositories;

namespace MapHive.Singletons
{
    public class ConfigurationSingleton : IConfigurationSingleton
    {
        private readonly IConfigurationRepository _configurationRepository;

        public ConfigurationSingleton(IConfigurationRepository configRepository)
        {
            this._configurationRepository = configRepository;
        }

        public async Task<ConfigurationItem?> GetConfigurationItemAsync(string key)
        {
            return await Task.Run(() => this._configurationRepository.GetConfigurationItemAsync(key));
        }

        public async Task<List<ConfigurationItem>> GetAllConfigurationItemsAsync()
        {
            return await Task.Run(this._configurationRepository.GetAllConfigurationItemsAsync);
        }

        public async Task<int> AddConfigurationItemAsync(ConfigurationItem item)
        {
            return await Task.Run(() => this._configurationRepository.AddConfigurationItemAsync(item));
        }

        public async Task<int> UpdateConfigurationItemAsync(ConfigurationItem item)
        {
            return await Task.Run(() => this._configurationRepository.UpdateConfigurationItemAsync(item));
        }

        public async Task<int> DeleteConfigurationItemAsync(string key)
        {
            return await this._configurationRepository.DeleteConfigurationItemAsync(key);
        }

        // Added methods from ConfigService

        public async Task<T?> GetConfigValueOrDefaultAsync<T>(string key, T? defaultValue = default)
        {
            ConfigurationItem? item = await this.GetConfigurationItemAsync(key);
            if (item != null)
            {
                try
                {
                    if (typeof(T) == typeof(bool) && item.Value is string stringVal)
                    {
                        if (int.TryParse(stringVal, out int intVal))
                        {
                            return (T)(object)(intVal != 0);
                        }
                        if (bool.TryParse(stringVal, out bool boolVal))
                        {
                            return (T)(object)boolVal;
                        }
                    }
                    return (T?)Convert.ChangeType(item.Value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        public async Task<bool> GetDevelopmentModeAsync()
        {
            return await this.GetConfigValueOrDefaultAsync("DevelopmentMode", false);
        }

        public async Task<string> GetTitleAsync()
        {
            return await this.GetConfigValueOrDefaultAsync("Title", "MapHive");
        }

        public async Task<string> GetFooterTextAsync()
        {
            return await this.GetConfigValueOrDefaultAsync("FooterText", "Default Footer Text");
        }

        public async Task<string?> GetConfigurationValueAsync(string key)
        {
            return await this._configurationRepository.GetConfigurationValueAsync(key);
        }
    }
}