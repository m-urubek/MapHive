namespace MapHive.Repositories;

using System.Data;
using System.Data.SQLite;
using System.Text.RegularExpressions;
using MapHive.Models.PageModels;
using MapHive.Singletons;

public partial class EditPageRepository(ISqlClientSingleton sqlClientSingleton) : IEditPageRepository
{
    private readonly ISqlClientSingleton _sqlClientSingleton = sqlClientSingleton;

    public async Task<EditItemPageModel> GetItemForEditOrThrowAsync(string tableName, int id)
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

        // Determine the ID column name
        string idColumn = await GetPrimaryKeyColumnAsync(tableName: tableName);

        // Build query to get all data for the specified item
        string query = $"SELECT * FROM \"{tableName}\" WHERE \"{idColumn}\" = @Id";
        SQLiteParameter[] parameters = [new("@Id", id)];

        // Execute query
        DataTable result = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);

        // If no item found with the specified ID, throw exception
        if (result.Rows.Count == 0)
            throw new Exception($"No data found for the specified ID \"{id}\" in table \"{tableName}\"");

        // Build the fields dictionary
        Dictionary<string, EditFieldInfo> fields = new();
        DataRow dataRow = result.Rows[0];

        foreach (DataRow schemaRow in schemaTable.Rows)
        {
            string columnName = schemaRow["name"].ToString() ?? string.Empty;
            string dataType = schemaRow["type"].ToString() ?? "TEXT";
            bool isPrimaryKey = Convert.ToInt32(schemaRow["pk"]) > 0;
            bool isNullable = Convert.ToInt32(schemaRow["notnull"]) == 0;

            // Check if this column is a foreign key
            bool isForeignKey = foreignKeys.AsEnumerable()
                .Any(fkRow => fkRow["from"].ToString() == columnName);

            // Get the current value
            object? value = dataRow[columnName];
            string valueStr = value == DBNull.Value ? string.Empty : value?.ToString() ?? string.Empty;

            // Format the column name for display
            string displayName = FormatColumnNameForDisplay(columnName: columnName);

            fields[columnName] = new EditFieldInfo
            {
                ColumnName = columnName,
                DisplayName = displayName,
                Value = valueStr,
                IsPrimaryKey = isPrimaryKey,
                IsForeignKey = isForeignKey,
                DataType = dataType,
                IsNullable = isNullable
            };
        }

        return new EditItemPageModel
        {
            TableName = tableName,
            ItemId = id,
            Fields = fields
        };
    }

    public async Task UpdateItemAsync(string tableName, int id, Dictionary<string, string?> updatedValues)
    {
        // Validate table name
        if (!IsValidTableName(tableName: tableName))
        {
            throw new ArgumentException("Invalid table name");
        }

        if (updatedValues == null || updatedValues.Count == 0)
        {
            throw new ArgumentException("No values provided for update");
        }

        // Get the primary key column
        string pkColumn = await GetPrimaryKeyColumnAsync(tableName: tableName);

        // Get table schema to validate column names and types
        DataTable schemaTable = await GetTableSchemaAsync(tableName: tableName);
        HashSet<string> validColumns = [.. schemaTable.AsEnumerable()
            .Select(row => row["name"].ToString() ?? string.Empty)];

        // Build SET clause
        List<string> setClauses = new();
        List<SQLiteParameter> parameters = new();

        foreach (KeyValuePair<string, string?> kvp in updatedValues)
        {
            string columnName = kvp.Key;

            // Skip if not a valid column or is the primary key
            if (!validColumns.Contains(columnName) || columnName == pkColumn)
                continue;

            // Validate column name
            if (!IsValidColumnName(columnName: columnName))
                continue;

            string paramName = $"@{columnName.Replace(oldValue: ".", newValue: "_")}";
            setClauses.Add($"\"{columnName}\" = {paramName}");

            // Handle null/empty values
            object paramValue = string.IsNullOrEmpty(kvp.Value) ? DBNull.Value : kvp.Value;
            parameters.Add(new SQLiteParameter(paramName, paramValue));
        }

        if (setClauses.Count == 0)
        {
            throw new ArgumentException("No valid fields to update");
        }

        // Build and execute the update query
        string setClause = string.Join(", ", setClauses);
        string query = $"UPDATE \"{tableName}\" SET {setClause} WHERE \"{pkColumn}\" = @ItemId";
        parameters.Add(new SQLiteParameter("@ItemId", id));

        int rowsAffected = await _sqlClientSingleton.UpdateOrThrowAsync(query: query, parameters: [.. parameters]);

        if (rowsAffected == 0)
        {
            throw new Exception($"No rows were updated for ID \"{id}\" in table \"{tableName}\"");
        }
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

