namespace MapHive.Repositories
{
    using MapHive.Models.RepositoryModels;

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