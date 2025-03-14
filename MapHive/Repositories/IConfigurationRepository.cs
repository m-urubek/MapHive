using MapHive.Models;

namespace MapHive.Repositories
{
    public interface IConfigurationRepository
    {
        ConfigurationItem? GetConfigurationItem(string key);
        List<ConfigurationItem> GetAllConfigurationItems();
        int AddConfigurationItem(ConfigurationItem item);
        int UpdateConfigurationItem(ConfigurationItem item);
        AppSettings GetAppSettings();
    }
}