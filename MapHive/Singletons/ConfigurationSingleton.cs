namespace MapHive.Singletons;

using System.Data;
using System.Data.SQLite;
using MapHive.Utilities;

public class ConfigurationSingleton(ISqlClientSingleton _sqlClientSingleton) : IConfigurationSingleton
{
    public async Task<string> GetConfigurationValueAsync(string key)
    {
        string query = "SELECT Value FROM Configuration WHERE Key = @Key";
        SQLiteParameter[] parameters = [new SQLiteParameter("@Key", key)];
        DataTable dataTable = await _sqlClientSingleton.SelectAsync(query, parameters);
        return dataTable.Rows[0]["Value"].ToString() ?? throw new Exception($"Configuration value \"{key}\" not found");
    }
    public async Task<bool> DevelopmentModeAsync()
    {
        string value = await GetConfigurationValueAsync("DevelopmentMode");
        try
        {
            return BooleanParser.Parse(value: value);
        }
        catch (Exception ex)
        {
            throw new Exception($"Configuration value DevelopmentMode is not a boolean", ex);
        }
    }
}
