namespace MapHive.Repositories;

using System.Data;
using System.Data.SQLite;
using System.Text.RegularExpressions;
using MapHive.Models.PageModels;
using MapHive.Singletons;

public partial class CreatePageRepository(ISqlClientSingleton sqlClientSingleton) : ICreatePageRepository
{
    private readonly ISqlClientSingleton _sqlClientSingleton = sqlClientSingleton;

    public async Task<CreateItemPageModel> GetTableSchemaForCreateAsync(string tableName)
    {
        // Validate table name to prevent SQL injection
        if (!IsValidTableName(tableName: tableName))
        {
            throw new ArgumentException("Invalid table name");
        }

        // Get table schema
        DataTable schemaTable = await GetTableSchemaAsync(tableName: tableName);

        // Get foreign keys info
        DataTable foreignKeys = await GetForeignKeysAsync(tableName: tableName);

        // Build the fields dictionary
        Dictionary<string, CreateFieldInfo> fields = new();

        foreach (DataRow schemaRow in schemaTable.Rows)
        {
            string columnName = schemaRow["name"].ToString() ?? string.Empty;
            string dataType = schemaRow["type"].ToString() ?? "TEXT";
            bool isPrimaryKey = Convert.ToInt32(schemaRow["pk"]) > 0;
            bool isNullable = Convert.ToInt32(schemaRow["notnull"]) == 0;
            string? defaultValue = schemaRow["dflt_value"] == DBNull.Value ? null : schemaRow["dflt_value"]?.ToString();

            // Check if this column is a foreign key
            bool isForeignKey = foreignKeys.AsEnumerable()
                .Any(fkRow => fkRow["from"].ToString() == columnName);

            // Format the column name for display
            string displayName = FormatColumnNameForDisplay(columnName: columnName);

            fields[columnName] = new CreateFieldInfo
            {
                ColumnName = columnName,
                DisplayName = displayName,
                DefaultValue = defaultValue,
                IsPrimaryKey = isPrimaryKey,
                IsForeignKey = isForeignKey,
                DataType = dataType,
                IsNullable = isNullable
            };
        }

        return new CreateItemPageModel
        {
            TableName = tableName,
            Fields = fields
        };
    }

    public async Task<int> CreateItemAsync(string tableName, Dictionary<string, string?> values)
    {
        // Validate table name
        if (!IsValidTableName(tableName: tableName))
        {
            throw new ArgumentException("Invalid table name");
        }

        if (values == null || values.Count == 0)
        {
            throw new ArgumentException("No values provided for insert");
        }

        // Get the primary key column (to exclude from insert)
        string pkColumn = await GetPrimaryKeyColumnAsync(tableName: tableName);

        // Get table schema to validate column names
        DataTable schemaTable = await GetTableSchemaAsync(tableName: tableName);
        HashSet<string> validColumns = [.. schemaTable.AsEnumerable()
            .Select(row => row["name"].ToString() ?? string.Empty)];

        // Build INSERT statement
        List<string> columnNames = new();
        List<string> paramNames = new();
        List<SQLiteParameter> parameters = new();

        foreach (KeyValuePair<string, string?> kvp in values)
        {
            string columnName = kvp.Key;

            // Skip if not a valid column or is the primary key (auto-increment)
            if (!validColumns.Contains(columnName) || columnName == pkColumn)
                continue;

            // Validate column name
            if (!IsValidColumnName(columnName: columnName))
                continue;

            string paramName = $"@{columnName.Replace(oldValue: ".", newValue: "_")}";
            columnNames.Add($"\"{columnName}\"");
            paramNames.Add(paramName);

            // Handle null/empty values
            object paramValue = string.IsNullOrEmpty(kvp.Value) ? DBNull.Value : kvp.Value;
            parameters.Add(new SQLiteParameter(paramName, paramValue));
        }

        if (columnNames.Count == 0)
        {
            throw new ArgumentException("No valid fields to insert");
        }

        // Build and execute the insert query
        string columnsClause = string.Join(", ", columnNames);
        string valuesClause = string.Join(", ", paramNames);
        string query = $"INSERT INTO \"{tableName}\" ({columnsClause}) VALUES ({valuesClause}); SELECT last_insert_rowid();";

        // Execute insert and get the new ID
        DataTable result = await _sqlClientSingleton.SelectAsync(query: query, parameters: [.. parameters]);

        if (result.Rows.Count == 0)
        {
            throw new Exception($"Failed to create new item in table \"{tableName}\"");
        }

        return Convert.ToInt32(result.Rows[0][0]);
    }

    public async Task<string> GetPrimaryKeyColumnAsync(string tableName)
    {
        // Validate table name
        if (!IsValidTableName(tableName: tableName))
        {
            throw new ArgumentException("Invalid table name for schema lookup");
        }

        // Get table schema
        DataTable schemaTable = await GetTableSchemaAsync(tableName: tableName);

        // Check for Id_{tableName} column first (project convention)
        string expectedIdColumn = $"Id_{tableName}";
        foreach (DataRow row in schemaTable.Rows)
        {
            string columnName = row["name"].ToString() ?? string.Empty;
            if (columnName.Equals(expectedIdColumn, StringComparison.OrdinalIgnoreCase))
            {
                return columnName;
            }
        }

        // Look for a column marked as primary key
        foreach (DataRow row in schemaTable.Rows)
        {
            if (Convert.ToInt32(row["pk"]) > 0)
            {
                return row["name"].ToString() ?? "Id";
            }
        }

        // Fallback to first column
        return schemaTable.Rows.Count > 0
            ? schemaTable.Rows[0]["name"].ToString() ?? "Id"
            : "Id";
    }

    private async Task<DataTable> GetTableSchemaAsync(string tableName)
    {
        string query = $"PRAGMA table_info(\"{tableName}\")";
        return await _sqlClientSingleton.SelectAsync(query: query);
    }

    private async Task<DataTable> GetForeignKeysAsync(string tableName)
    {
        string query = $"PRAGMA foreign_key_list(\"{tableName}\")";
        return await _sqlClientSingleton.SelectAsync(query: query);
    }

    private static string FormatColumnNameForDisplay(string columnName)
    {
        // Remove Id_ prefix if exists
        if (columnName.StartsWith("Id_", StringComparison.OrdinalIgnoreCase))
        {
            columnName = columnName[3..];
        }

        // Insert space before capital letters
        return CamelCaseRegex().Replace(input: columnName, replacement: " ");
    }

    private static bool IsValidTableName(string tableName)
    {
        return !string.IsNullOrWhiteSpace(tableName) && AlphanumericRegex().IsMatch(tableName);
    }

    private static bool IsValidColumnName(string columnName)
    {
        return !string.IsNullOrWhiteSpace(columnName) && AlphanumericRegex().IsMatch(columnName);
    }

    [GeneratedRegex("(?<=[a-z])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])")]
    private static partial Regex CamelCaseRegex();

    [GeneratedRegex(@"^[a-zA-Z0-9_]+$")]
    private static partial Regex AlphanumericRegex();
}

