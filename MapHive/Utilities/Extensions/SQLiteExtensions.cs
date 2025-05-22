namespace MapHive;

using System.Data.SQLite;

public static class SQLiteExtensions
{
    public static void AddIfNotNullOrWhitespace(this List<SQLiteParameter> parameters, string key, object? value)
    {
        if (value != null && value != DBNull.Value && !string.IsNullOrWhiteSpace(value.ToString()))
        {
            parameters.Add(new SQLiteParameter(key, value));
        }
    }
}
