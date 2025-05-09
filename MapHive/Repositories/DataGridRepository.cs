using MapHive.Models.BusinessModels;
using MapHive.Models.RepositoryModels;
using MapHive.Singletons;
using System.Data;
using System.Data.SQLite;
using System.Text.RegularExpressions;

namespace MapHive.Repositories
{
    public class DataGridRepository : IDataGridRepository
    {
        private readonly ISqlClientSingleton _sqlClientSingleton;

        public DataGridRepository(ISqlClientSingleton sqlClientSingleton)
        {
            this._sqlClientSingleton = sqlClientSingleton;
        }

        // Get schema information for a table
        public async Task<DataTable> GetTableSchemaAsync(string tableName)
        {
            // Sanitize table name to prevent SQL injection
            if (!IsValidTableName(tableName))
            {
                throw new ArgumentException("Invalid table name");
            }

            string query = $"PRAGMA table_info({tableName})";
            return await this._sqlClientSingleton.SelectAsync(query);
        }

        // Get schema information about foreign keys for a table
        private async Task<DataTable> GetForeignKeysAsync(string tableName)
        {
            // Sanitize table name to prevent SQL injection
            if (!IsValidTableName(tableName))
            {
                throw new ArgumentException("Invalid table name");
            }

            string query = $"PRAGMA foreign_key_list({tableName})";
            return await this._sqlClientSingleton.SelectAsync(query);
        }

        // Get columns for a table
        public async Task<List<DataGridColumnGet>> GetColumnsForTableAsync(string tableName)
        {
            DataTable schemaTable = await this.GetTableSchemaAsync(tableName);
            DataTable foreignKeysTable = await this.GetForeignKeysAsync(tableName);

            List<DataGridColumnGet> columns = new();

            for (int i = 0; i < schemaTable.Rows.Count; i++)
            {
                DataRow row = schemaTable.Rows[i];
                string columnName = row["name"].ToString() ?? string.Empty;

                DataGridColumnGet column = new()
                {
                    DisplayName = columnName, //translate later
                    InternalName = columnName,
                    Flex = "1 1 auto",
                    Index = i
                };

                // Check if this column is a foreign key
                bool isForeignKey = foreignKeysTable.AsEnumerable()
                                        .Any(fkRow => fkRow["from"].ToString() == columnName);

                columns.Add(column);
            }

            // Mark the last column
            if (columns.Count > 0)
            {
                columns[^1].IsLastColumn = true;
            }

            return columns;
        }

        // Get information about a column for search
        public async Task<ColumnInfo> GetColumnInfoAsync(string tableName, string columnName)
        {
            // Sanitize table and column names
            if (!IsValidTableName(tableName) || !IsValidColumnName(columnName))
            {
                throw new ArgumentException("Invalid table or column name");
            }

            ColumnInfo searchInfo = new()
            {
                TableName = tableName,
                ColumnName = columnName
            };

            // Check if this column is a foreign key
            DataTable foreignKeysTable = await this.GetForeignKeysAsync(tableName);
            DataRow? fkRow = foreignKeysTable.AsEnumerable()
                                .FirstOrDefault(row => row["from"].ToString() == columnName);

            if (fkRow != null)
            {
                searchInfo.IsForeignKey = true;
                searchInfo.ForeignTable = fkRow["table"]?.ToString();
                searchInfo.ForeignColumn = fkRow["to"]?.ToString();
            }

            return searchInfo;
        }

        // Get grid data with pagination, sorting, and searching
        public async Task<DataGridGet> GetGridDataAsync(
            string tableName,
            int page = 1,
            int pageSize = 20,
            string searchTerm = "",
            string sortField = "",
            string sortDirection = "asc")
        {
            // Validate parameters
            if (!IsValidTableName(tableName))
            {
                throw new ArgumentException("Invalid table name");
            }

            if (!string.IsNullOrEmpty(sortField) && !IsValidColumnName(sortField))
            {
                throw new ArgumentException("Invalid sort field");
            }

            // Ensure page and pageSize are valid
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100); // Example clamp

            // Initialize view model
            DataGridGet viewModel = new()
            {
                TableName = tableName,
                CurrentPage = page,
                PageSize = pageSize,
                SearchTerm = searchTerm,
                SortField = sortField,
                SortDirection = sortDirection.ToLowerInvariant() == "desc" ? "desc" : "asc", // Sanitize direction
                // Get table columns
                Columns = await this.GetColumnsForTableAsync(tableName)
            };

            // Build query
            (string query, List<SQLiteParameter> parameters) = await this.BuildQueryAsync(
                tableName,
                viewModel.Columns, // Pass columns to avoid re-fetching
                page,
                pageSize,
                searchTerm,
                sortField,
                viewModel.SortDirection
            );

            // Execute query to get data
            DataTable dataTable = await this._sqlClientSingleton.SelectAsync(query, parameters.ToArray());

            // Convert data to grid rows
            viewModel.Items = ConvertDataTableToGridRows(dataTable, viewModel.Columns);

            // Get total count for pagination
            viewModel.TotalCount = await this.GetTotalRowsCountAsync(tableName, searchTerm, viewModel.Columns);

            return viewModel;
        }

        // Helper method to convert DataTable to grid rows
        private static List<DataGridRowGet> ConvertDataTableToGridRows(DataTable dataTable, List<DataGridColumnGet> columns)
        {
            List<DataGridRowGet> rows = new();
            string? idColumnName = columns.FirstOrDefault(c => c.InternalName.StartsWith("Id_"))?.InternalName;

            foreach (DataRow dataRow in dataTable.Rows)
            {
                DataGridRowGet gridRow = new();

                // Assign RowId if an ID column exists and has a value
                if (!string.IsNullOrEmpty(idColumnName) && dataTable.Columns.Contains(idColumnName) && dataRow[idColumnName] != DBNull.Value)
                {
                    if (int.TryParse(dataRow[idColumnName].ToString(), out int idValue))
                    {
                        gridRow.RowId = idValue;
                    }
                }

                foreach (DataGridColumnGet column in columns)
                {
                    string columnName = column.InternalName;
                    if (dataTable.Columns.Contains(columnName))
                    {
                        string cellContent = dataRow[columnName]?.ToString() ?? string.Empty;
                        gridRow.CellsByColumnNames[columnName] = new DataGridCellGet { Content = cellContent };
                    }
                    else
                    {
                        // Handle cases where a column might be expected but not in the result (e.g., calculated columns)
                        gridRow.CellsByColumnNames[columnName] = new DataGridCellGet { Content = string.Empty }; // Or handle differently
                    }
                }
                rows.Add(gridRow);
            }
            return rows;
        }

        // Get total count of rows matching the search criteria
        public async Task<int> GetTotalRowsCountAsync(string tableName, string searchTerm, List<DataGridColumnGet> columns)
        {
            // Validate
            if (!IsValidTableName(tableName))
            {
                throw new ArgumentException("Invalid table name");
            }

            // Build WHERE clause for counting
            (string whereClause, List<SQLiteParameter> parameters) = this.BuildWhereClause(searchTerm, columns);

            string query = $"SELECT COUNT(*) FROM {tableName} {whereClause}";

            // Execute query
            DataTable resultTable = await this._sqlClientSingleton.SelectAsync(query, parameters.ToArray());

            return resultTable.Rows.Count > 0 && resultTable.Rows[0][0] != DBNull.Value ? Convert.ToInt32(resultTable.Rows[0][0]) : 0;
        }

        // Build the main SQL query for fetching grid data
        private async Task<(string query, List<SQLiteParameter> parameters)> BuildQueryAsync(
            string tableName,
            List<DataGridColumnGet> columns,
            int page,
            int pageSize,
            string searchTerm,
            string sortField,
            string sortDirection)
        {
            // Build WHERE clause
            (string whereClause, List<SQLiteParameter> parameters) = this.BuildWhereClause(searchTerm, columns);

            // Build ORDER BY clause
            string orderByClause = string.Empty;
            if (!string.IsNullOrEmpty(sortField) && IsValidColumnName(sortField) && columns.Any(c => c.InternalName == sortField))
            {
                string direction = sortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
                orderByClause = $"ORDER BY \"{sortField}\" {direction}"; // Quote column name
            }
            else
            {
                // Default sort by the first column (usually ID) if available
                string? firstColumn = columns.FirstOrDefault(c => c.InternalName.StartsWith("Id_"))?.InternalName ?? columns.FirstOrDefault()?.InternalName;
                if (!string.IsNullOrEmpty(firstColumn))
                {
                    orderByClause = $"ORDER BY \"{firstColumn}\" ASC"; // Quote column name
                }
            }

            // Build LIMIT/OFFSET clause for pagination
            int offset = (page - 1) * pageSize;
            string limitClause = $"LIMIT @PageSize OFFSET @Offset";
            parameters.Add(new SQLiteParameter("@PageSize", pageSize));
            parameters.Add(new SQLiteParameter("@Offset", offset));

            // Select only the columns needed for the grid
            string selectColumns = string.Join(", ", columns.Select(c => $"\"{c.InternalName}\"")); // Quote column names
            if (!columns.Any(c => c.InternalName.StartsWith("Id_"))) // Ensure ID column is selected if not already included
            {
                DataRow? idCol = (await this.GetTableSchemaAsync(tableName)).AsEnumerable().FirstOrDefault(r => r["name"].ToString().StartsWith("Id_"));
                if (idCol != null && !selectColumns.Contains($"\"{idCol["name"]}\""))
                {
                    selectColumns = $"\"{idCol["name"]}\", {selectColumns}";
                }
            }
            if (string.IsNullOrEmpty(selectColumns))
            {
                selectColumns = "*"; // Fallback if no columns somehow
            }

            string query = $"SELECT {selectColumns} FROM {tableName} {whereClause} {orderByClause} {limitClause}";

            return (query, parameters);
        }

        // Build the WHERE clause based on the search term
        private (string whereClause, List<SQLiteParameter> parameters) BuildWhereClause(
            string searchTerm,
            List<DataGridColumnGet> columns)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return (string.Empty, new List<SQLiteParameter>());
            }

            List<string> conditions = new();
            List<SQLiteParameter> parameters = new();
            int paramIndex = 0;

            // Use only the columns provided for searching
            foreach (DataGridColumnGet column in columns)
            {
                // Maybe add type checking here later to avoid searching non-text fields?
                string paramName = $"@SearchParam{paramIndex++}";
                conditions.Add($"CAST(\"{column.InternalName}\" AS TEXT) LIKE {paramName}"); // Quote column name
                parameters.Add(new SQLiteParameter(paramName, $"%{searchTerm}%"));
            }

            if (conditions.Any())
            {
                return ($"WHERE " + string.Join(" OR ", conditions), parameters);
            }
            else
            {
                return (string.Empty, parameters); // No searchable columns found
            }
        }

        // Validate table name to prevent SQL injection
        private static bool IsValidTableName(string tableName)
        {
            return !string.IsNullOrWhiteSpace(tableName) && Regex.IsMatch(tableName, @"^[a-zA-Z0-9_]+$");
        }

        // Validate column name to prevent SQL injection
        private static bool IsValidColumnName(string columnName)
        {
            return !string.IsNullOrWhiteSpace(columnName) && Regex.IsMatch(columnName, @"^[a-zA-Z0-9_]+$");
        }
    }
}