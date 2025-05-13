namespace MapHive.Services
{
    using MapHive.Models.RepositoryModels;

    public interface IConfigurationService
    {
        Task<List<ConfigurationItem>> GetAllConfigurationItemsAsync();
        Task<ConfigurationItem?> GetConfigurationItemAsync(string key);
        Task<string?> GetConfigurationValueAsync(string key);
        Task<int> AddConfigurationItemAsync(ConfigurationItem item);
        Task<int> UpdateConfigurationItemAsync(ConfigurationItem item);
        Task<int> DeleteConfigurationItemAsync(string key);
        Task<bool> GetDevelopmentModeAsync();
    }
}