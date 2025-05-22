namespace MapHive.Singletons;

public interface IConfigurationSingleton
{
    Task<string> GetConfigurationValueAsync(string key);
    Task<bool> DevelopmentModeAsync();
}
