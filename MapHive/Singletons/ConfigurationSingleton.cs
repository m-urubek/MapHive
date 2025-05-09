namespace MapHive.Singletons
{
    using MapHive.Models.RepositoryModels;
    using MapHive.Repositories;

    public class ConfigurationSingleton(IConfigurationRepository configRepository) : IConfigurationSingleton
    {
        private readonly IConfigurationRepository _configurationRepository = configRepository;

        public async Task<ConfigurationItem?> GetConfigurationItemAsync(string key)
        {
            return await Task.Run(function: () => _configurationRepository.GetConfigurationItemAsync(key: key));
        }

        public async Task<List<ConfigurationItem>> GetAllConfigurationItemsAsync()
        {
            return await Task.Run(function: _configurationRepository.GetAllConfigurationItemsAsync);
        }

        public async Task<int> AddConfigurationItemAsync(ConfigurationItem item)
        {
            return await Task.Run(function: () => _configurationRepository.AddConfigurationItemAsync(item: item));
        }

        public async Task<int> UpdateConfigurationItemAsync(ConfigurationItem item)
        {
            return await Task.Run(function: () => _configurationRepository.UpdateConfigurationItemAsync(item: item));
        }

        public async Task<int> DeleteConfigurationItemAsync(string key)
        {
            return await _configurationRepository.DeleteConfigurationItemAsync(key: key);
        }

        // Added methods from ConfigService

        public async Task<T> GetConfigValueOrDefaultAsync<T>(string key)
        {
            ConfigurationItem item = await GetConfigurationItemAsync(key: key) ?? throw new Exception($"Configuration item \"{key}\" not found");
            try
            {
                if (typeof(T) == typeof(bool) && item.Value is string stringVal)
                {
                    if (int.TryParse(s: stringVal, result: out int intVal))
                    {
                        return (T)(object)(intVal != 0);
                    }
                    if (bool.TryParse(value: stringVal, result: out bool boolVal))
                    {
                        return (T)(object)boolVal;
                    }
                }
                return (T)Convert.ChangeType(value: item.Value, conversionType: typeof(T));
            }
            catch (Exception ex)
            {
                throw new Exception($"Unexpected error while retreiving consifguration value \"{key}\"", ex);
            }
        }

        public async Task<bool> GetDevelopmentModeAsync()
        {
            return await GetConfigValueOrDefaultAsync<bool>(key: "DevelopmentMode");
        }

        public async Task<string> GetTitleAsync()
        {
            return await GetConfigValueOrDefaultAsync<string>(key: "Title");
        }

        public async Task<string> GetFooterTextAsync()
        {
            return await GetConfigValueOrDefaultAsync<string>(key: "FooterText");
        }

        public async Task<string?> GetConfigurationValueAsync(string key)
        {
            return await _configurationRepository.GetConfigurationValueAsync(key: key);
        }
    }
}
