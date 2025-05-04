using MapHive.Singletons;
using System.Data;
using System.Data.SQLite;
using System.Text.RegularExpressions;

namespace MapHive.Repositories
{
    public class DisplayRepository : IDisplayRepository
    {
        private readonly ISqlClientSingleton _sqlClient;

        public DisplayRepository(ISqlClientSingleton sqlClient)
        {
            this._sqlClient = sqlClient;
        }

        public async Task<Dictionary<string, string>> GetItemDataAsync(string tableName, int id)
        {
            // Validate table name to prevent SQL injection
            if (!IsValidTableName(tableName))
            {
                throw new ArgumentException("Invalid table name");
            }

            // Determine the ID column name (first column or Id_{tableName})
            string idColumn = await this.GetIdColumnNameAsync(tableName);

            // Build query to get all data for the specified item
            string query = $"SELECT * FROM \"{tableName}\" WHERE \"{idColumn}\" = @Id";
            SQLiteParameter[] parameters = new SQLiteParameter[] { new("@Id", id) };

            // Execute query using injected _sqlClient
            DataTable result = await this._sqlClient.SelectAsync(query, parameters);

            // If no item found with the specified ID, return empty dictionary
            if (result.Rows.Count == 0)
            {
                return new Dictionary<string, string>();
            }

            // Convert DataRow to Dictionary
            return ConvertDataRowToDictionary(result.Rows[0], result.Columns);
        }

        public async Task<bool> TableExistsAsync(string tableName)
        {
            // Validate table name to prevent SQL injection
            if (!IsValidTableName(tableName))
            {
                throw new ArgumentException("Invalid table name");
            }

            // Query to check if table exists
            string query = "SELECT name FROM sqlite_master WHERE type='table' AND name=@TableName";
            SQLiteParameter[] parameters = new SQLiteParameter[] { new("@TableName", tableName) };

            // Execute query using injected _sqlClient
            DataTable result = await this._sqlClient.SelectAsync(query, parameters);

            // Return true if table exists
            return result.Rows.Count > 0;
        }

        private async Task<string> GetIdColumnNameAsync(string tableName)
        {
            // Validate table name
            if (!IsValidTableName(tableName))
            {
                throw new ArgumentException("Invalid table name for schema lookup");
            }
            // Get table schema using injected _sqlClient
            string query = $"PRAGMA table_info(\"{tableName}\")";
            DataTable schemaTable = await this._sqlClient.SelectAsync(query);

            // Check for Id_{tableName} column
            string expectedIdColumn = $"Id_{tableName}";
            foreach (DataRow row in schemaTable.Rows)
            {
                string columnName = row["name"].ToString() ?? string.Empty;
                if (columnName.Equals(expectedIdColumn, StringComparison.OrdinalIgnoreCase))
                {
                    return columnName;
                }
            }

            // If Id_{tableName} not found, return the first column name (usually the PK)
            if (schemaTable.Rows.Count > 0 && schemaTable.Rows[0]["pk"] != DBNull.Value && Convert.ToInt32(schemaTable.Rows[0]["pk"]) > 0)
            {
                return schemaTable.Rows[0]["name"].ToString() ?? "Id";
            }
            // Fallback if no explicit PK or specific Id_ pattern found
            else if (schemaTable.Rows.Count > 0)
            {
                return schemaTable.Rows[0]["name"].ToString() ?? "Id";
            }

            // Default to "Id" if no columns found (should ideally not happen for existing tables)
            return "Id";
        }

        private static Dictionary<string, string> ConvertDataRowToDictionary(DataRow row, DataColumnCollection columns)
        {
            Dictionary<string, string> result = new();

            foreach (DataColumn column in columns)
            {
                string columnName = column.ColumnName;
                string value = row[columnName]?.ToString() ?? string.Empty;

                // Format the column name for better display (remove Id_ prefix, add spaces between camel case)
                string displayName = FormatColumnNameForDisplay(columnName);

                // Only add non-ID columns to the display dictionary
                if (!columnName.StartsWith("Id_", StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(displayName, value);
                }
            }

            return result;
        }

        private static string FormatColumnNameForDisplay(string columnName)
        {
            // Remove Id_ prefix if exists
            if (columnName.StartsWith("Id_", StringComparison.OrdinalIgnoreCase))
            {
                columnName = columnName[3..];
            }

            // Insert space before capital letters, handle acronyms (like ID)
            return Regex.Replace(columnName, "(?<=[a-z])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", " ");
        }

        // Validate table name to prevent SQL injection
        private static bool IsValidTableName(string tableName)
        {
            // Allow alphanumeric and underscore
            return !string.IsNullOrWhiteSpace(tableName) &&
                   Regex.IsMatch(tableName, @"^[a-zA-Z0-9_]+$");
        }
    }
}