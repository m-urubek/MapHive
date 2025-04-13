using MapHive.Repositories.Interfaces;
using MapHive.Singletons;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Threading.Tasks;

namespace MapHive.Repositories
{
    public class DisplayRepository : IDisplayRepository
    {
        public async Task<Dictionary<string, string>> GetItemDataAsync(string tableName, int id)
        {
            // Validate table name to prevent SQL injection
            if (!IsValidTableName(tableName))
            {
                throw new ArgumentException("Invalid table name");
            }

            // Determine the ID column name (first column or Id_{tableName})
            string idColumn = await GetIdColumnNameAsync(tableName);

            // Build query to get all data for the specified item
            string query = $"SELECT * FROM {tableName} WHERE {idColumn} = @Id";
            SQLiteParameter[] parameters = new SQLiteParameter[] { new("@Id", id) };

            // Execute query
            DataTable result = await CurrentRequest.SqlClient.SelectAsync(query, parameters);

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

            // Execute query
            DataTable result = await CurrentRequest.SqlClient.SelectAsync(query, parameters);

            // Return true if table exists
            return result.Rows.Count > 0;
        }

        private async Task<string> GetIdColumnNameAsync(string tableName)
        {
            // Get table schema
            string query = $"PRAGMA table_info({tableName})";
            DataTable schemaTable = await CurrentRequest.SqlClient.SelectAsync(query);

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

            // If Id_{tableName} not found, return the first column name
            if (schemaTable.Rows.Count > 0)
            {
                return schemaTable.Rows[0]["name"].ToString() ?? "Id";
            }

            // Default to Id if no columns found (should never happen)
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
                
                result.Add(displayName, value);
            }

            return result;
        }

        private static string FormatColumnNameForDisplay(string columnName)
        {
            // Remove Id_ prefix if exists
            if (columnName.StartsWith("Id_", StringComparison.OrdinalIgnoreCase))
            {
                columnName = columnName.Substring(3);
            }

            // Insert space before capital letters to create a more readable format
            // e.g., "FirstName" becomes "First Name"
            string formatted = string.Empty;
            for (int i = 0; i < columnName.Length; i++)
            {
                if (i > 0 && char.IsUpper(columnName[i]))
                {
                    formatted += " ";
                }
                formatted += columnName[i];
            }

            return formatted;
        }

        // Validate table name to prevent SQL injection
        private static bool IsValidTableName(string tableName)
        {
            return !string.IsNullOrWhiteSpace(tableName) &&
                   System.Text.RegularExpressions.Regex.IsMatch(tableName, @"^[a-zA-Z0-9_]+$");
        }
    }
} 