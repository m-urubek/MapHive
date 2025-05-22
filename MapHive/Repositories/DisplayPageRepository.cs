namespace MapHive.Repositories;

using System.Data;
using System.Data.SQLite;
using System.Text.RegularExpressions;
using MapHive.Singletons;

public partial class DisplayPageRepository(ISqlClientSingleton sqlClientSingleton) : IDisplayPageRepository
{
    private readonly ISqlClientSingleton _sqlClientSingleton = sqlClientSingleton;

    public async Task<Dictionary<string, string>> GetItemDataOrThrowAsync(string tableName, int id)
    {
        // Validate table name to prevent SQL injection
        if (!IsValidTableName(tableName: tableName))
        {
            throw new ArgumentException("Invalid table name");
        }

        // Determine the ID column name (first column or Id_{tableName})
        string idColumn = await GetIdColumnNameAsync(tableName: tableName);

        // Build query to get all data for the specified item
        string query = $"SELECT * FROM \"{tableName}\" WHERE \"{idColumn}\" = @Id";
        SQLiteParameter[] parameters = [new("@Id", id)];

        // Execute query using injected _sqlClientSingleton
        DataTable result = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);

        // If no item found with the specified ID, return empty dictionary
        if (result.Rows.Count == 0)
            throw new Exception($"No data found for the specified ID \"{id}\" in table \"{tableName}\"");

        // Convert DataRow to Dictionary
        return ConvertDataRowToDictionary(row: result.Rows[0], columns: result.Columns);
    }

    public async Task<bool> TableExistsAsync(string tableName)
    {
        // Validate table name to prevent SQL injection
        if (!IsValidTableName(tableName: tableName))
        {
            throw new ArgumentException("Invalid table name");
        }

        // Query to check if table exists
        string query = "SELECT name FROM sqlite_master WHERE type='table' AND name=@TableName";
        SQLiteParameter[] parameters = [new("@TableName", tableName)];

        // Execute query using injected _sqlClientSingleton
        DataTable result = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);

        // Return true if table exists
        return result.Rows.Count > 0;
    }

    private async Task<string> GetIdColumnNameAsync(string tableName)
    {
        // Validate table name
        if (!IsValidTableName(tableName: tableName))
        {
            throw new ArgumentException("Invalid table name for schema lookup");
        }
        // Get table schema using injected _sqlClientSingleton
        string query = $"PRAGMA table_info(\"{tableName}\")";
        DataTable schemaTable = await _sqlClientSingleton.SelectAsync(query: query);

        // Check for Id_{tableName} column
        string expectedIdColumn = $"Id_{tableName}";
        foreach (DataRow row in schemaTable.Rows)
        {
            string columnName = row["name"].ToString() ?? string.Empty;
            if (columnName.Equals(value: expectedIdColumn, comparisonType: StringComparison.OrdinalIgnoreCase))
            {
                return columnName;
            }
        }

        // If Id_{tableName} not found, return the first column name (usually the PK)
        if (schemaTable.Rows.Count > 0 && schemaTable.Rows[0]["pk"] != DBNull.Value && Convert.ToInt32(value: schemaTable.Rows[0]["pk"]) > 0)
        {
            return schemaTable.Rows[0]["name"].ToString() ?? "Id";
        }
        // Fallback if no explicit PK or specific Id_ pattern found
        else if (schemaTable.Rows.Count > 0)
        {
            return schemaTable.Rows[0]["name"].ToString() ?? "Id";
        }

        return "Id";
    }

    private static Dictionary<string, string> ConvertDataRowToDictionary(DataRow row, DataColumnCollection columns)
    {
        Dictionary<string, string> result = new();

        foreach (DataColumn column in columns)
        {
            string columnName = column.ColumnName;
            string value = row.GetValueThrowNotPresent<object?>(columnName: columnName)?.ToString() ?? string.Empty;

            // Format the column name for better display (remove Id_ prefix, add spaces between camel case)
            string displayName = FormatColumnNameForDisplay(columnName: columnName);

            // Only add non-ID columns to the display dictionary
            if (!columnName.StartsWith(value: "Id_", comparisonType: StringComparison.OrdinalIgnoreCase))
            {
                result.Add(key: displayName, value: value);
            }
        }

        return result;
    }

    private static string FormatColumnNameForDisplay(string columnName)
    {
        // Remove Id_ prefix if exists
        if (columnName.StartsWith(value: "Id_", comparisonType: StringComparison.OrdinalIgnoreCase))
        {
            columnName = columnName[3..];
        }

        // Insert space before capital letters, handle acronyms (like ID)
        return MyRegex().Replace(input: columnName, replacement: " ");
    }

    // Validate table name to prevent SQL injection
    private static bool IsValidTableName(string tableName)
    {
        // Allow alphanumeric and underscore
        return !string.IsNullOrWhiteSpace(value: tableName) &&
               MyRegex1().IsMatch(input: tableName);
    }

    [GeneratedRegex("(?<=[a-z])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])")]
    private static partial Regex MyRegex();
    [GeneratedRegex(@"^[a-zA-Z0-9_]+$")]
    private static partial Regex MyRegex1();
}
