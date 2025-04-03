using MapHive.Models;

namespace MapHive.Services
{
    public interface IConfigService
    {
        Task<ConfigurationItem?> GetConfigurationItemAsync(string key);
        Task<List<ConfigurationItem>> GetAllConfigurationItemsAsync();
        Task<int> AddConfigurationItemAsync(ConfigurationItem item);
        Task<int> UpdateConfigurationItemAsync(ConfigurationItem item);
        Task<bool> DeleteConfigurationItemAsync(string key);
    }
} 