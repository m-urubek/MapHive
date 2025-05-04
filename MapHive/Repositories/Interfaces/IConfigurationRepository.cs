using MapHive.Models.RepositoryModels;

namespace MapHive.Repositories
{
    public interface IConfigurationRepository
    {
        Task<List<ConfigurationItem>> GetAllConfigurationItemsAsync();
        Task<ConfigurationItem?> GetConfigurationItemAsync(string key);
        Task<string?> GetConfigurationValueAsync(string key);
        Task<int> AddConfigurationItemAsync(ConfigurationItem item);
        Task<int> UpdateConfigurationItemAsync(ConfigurationItem item);
        Task<int> DeleteConfigurationItemAsync(string key);
    }
}