using MapHive.Models.DataGrid;
using MapHive.Repositories.Interfaces;
using MapHive.Singletons;
using System.Data;
using System.Data.SQLite;
using System.Text;

namespace MapHive.Repositories
{
    public class DataGridRepository : IDataGridRepository
    {
        // Get schema information for a table
        public async Task<DataTable> GetTableSchemaAsync(string tableName)
        {
            // Sanitize table name to prevent SQL injection
            if (!IsValidTableName(tableName))
            {
                throw new ArgumentException("Invalid table name");
            }

            string query = $"PRAGMA table_info({tableName})";
            return await CurrentRequest.SqlClient.SelectAsync(query);
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
            return await CurrentRequest.SqlClient.SelectAsync(query);
        }

        // Get columns for a table
        public async Task<List<DataGridColumn>> GetColumnsForTableAsync(string tableName)
        {
            DataTable schemaTable = await this.GetTableSchemaAsync(tableName);
            DataTable foreignKeysTable = await this.GetForeignKeysAsync(tableName);

            List<DataGridColumn> columns = new();

            for (int i = 0; i < schemaTable.Rows.Count; i++)
            {
                DataRow row = schemaTable.Rows[i];
                string columnName = row["name"].ToString() ?? string.Empty;

                // Skip internal ID columns
                if (columnName.StartsWith("Id_") && i == 0)
                {
                    continue;
                }

                DataGridColumn column = new()
                {
                    DisplayName = columnName, //translate later
                    InternalName = columnName,
                    Flex = "1 1 auto",
                    Index = i
                };

                // Check if this column is a foreign key
                foreach (DataRow fkRow in foreignKeysTable.Rows)
                {
                    string fromColumn = fkRow["from"].ToString() ?? string.Empty;
                    if (columnName == fromColumn)
                    {
                        break;
                    }
                }

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
        public async Task<SearchColumnInfo> GetSearchColumnInfoAsync(string tableName, string columnName)
        {
            // Sanitize table and column names
            if (!IsValidTableName(tableName) || !IsValidColumnName(columnName))
            {
                throw new ArgumentException("Invalid table or column name");
            }

            SearchColumnInfo searchInfo = new()
            {
                TableName = tableName,
                ColumnName = columnName
            };

            // Check if this column is a foreign key
            DataTable foreignKeysTable = await this.GetForeignKeysAsync(tableName);
            foreach (DataRow fkRow in foreignKeysTable.Rows)
            {
                string fromColumn = fkRow["from"].ToString() ?? string.Empty;
                if (columnName == fromColumn)
                {
                    searchInfo.IsForeignKey = true;
                    searchInfo.ForeignTable = fkRow["table"].ToString();
                    searchInfo.ForeignColumn = fkRow["to"].ToString();
                    break;
                }
            }

            return searchInfo;
        }

        // Get grid data with pagination, sorting, and searching
        public async Task<DataGrid> GetGridDataAsync(
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

            // Initialize view model
            DataGrid viewModel = new()
            {
                TableName = tableName,
                CurrentPage = page,
                PageSize = pageSize,
                SearchTerm = searchTerm,
                SortField = sortField,
                SortDirection = sortDirection,
                // Get table columns
                Columns = await this.GetColumnsForTableAsync(tableName)
            };

            // Build query
            (string query, List<SQLiteParameter> parameters) = await this.BuildQueryAsync(
                tableName,
                page,
                pageSize,
                searchTerm,
                sortField,
                sortDirection
            );

            // Execute query to get data
            DataTable dataTable = await CurrentRequest.SqlClient.SelectAsync(query, parameters.ToArray());

            // Convert data to grid rows
            viewModel.Items = ConvertDataTableToGridRows(dataTable, viewModel.Columns);

            // Get total count for pagination
            viewModel.TotalCount = await this.GetTotalRowsCountAsync(tableName, searchTerm);

            return viewModel;
        }

        // Helper method to convert DataTable to grid rows
        private static List<DataGridRow> ConvertDataTableToGridRows(DataTable dataTable, List<DataGridColumn> columns)
        {
            List<DataGridRow> rows = new();

            foreach (DataRow dataRow in dataTable.Rows)
            {
                DataGridRow row = new();

                foreach (DataGridColumn column in columns)
                {
                    string columnName = column.InternalName;

                    if (!dataTable.Columns.Contains(columnName))
                    {
                        continue;
                    }

                    string cellContent = dataRow[columnName]?.ToString() ?? string.Empty;

                    DataGridCell cell = new()
                    {
                        Content = cellContent,
                        Flex = column.Flex
                    };

                    row.CellsByColumnNames[columnName] = cell;
                }

                rows.Add(row);
            }

            return rows;
        }

        // Get total count of rows for pagination
        public async Task<int> GetTotalRowsCountAsync(string tableName, string searchTerm = "")
        {
            // Validate tableName
            if (!IsValidTableName(tableName))
            {
                throw new ArgumentException("Invalid table name");
            }

            // Build WHERE clause
            (string whereClause, List<SQLiteParameter> parameters) = await this.BuildWhereClauseAsync(tableName, searchTerm);

            // Build count query
            string query = $"SELECT COUNT(*) FROM {tableName} {whereClause}";

            // Execute query
            DataTable resultTable = await CurrentRequest.SqlClient.SelectAsync(query, parameters.ToArray());

            // Get total count
            return Convert.ToInt32(resultTable.Rows[0][0]);
        }

        // Build SQL query with search, sorting, and pagination
        private async Task<(string query, List<SQLiteParameter> parameters)> BuildQueryAsync(
            string tableName,
            int page,
            int pageSize,
            string searchTerm,
            string sortField,
            string sortDirection)
        {
            // Build WHERE clause
            (string whereClause, List<SQLiteParameter> parameters) = await this.BuildWhereClauseAsync(tableName, searchTerm);

            // Build ORDER BY clause
            string orderByClause = "";
            if (!string.IsNullOrEmpty(sortField) && IsValidColumnName(sortField))
            {
                string direction = sortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
                orderByClause = $"ORDER BY {sortField} {direction}";
            }

            // Calculate offset for pagination
            int offset = (page - 1) * pageSize;

            // Build simple SELECT 
            string query = $"SELECT {tableName}.* FROM {tableName} {whereClause} {orderByClause} LIMIT @PageSize OFFSET @Offset";

            // Add pagination parameters
            parameters.Add(new SQLiteParameter("@PageSize", pageSize));
            parameters.Add(new SQLiteParameter("@Offset", offset));

            return (query, parameters);
        }

        // Build WHERE clause for search
        private async Task<(string whereClause, List<SQLiteParameter> parameters)> BuildWhereClauseAsync(
            string tableName,
            string searchTerm)
        {
            List<string> conditions = new();
            List<SQLiteParameter> parameters = new();

            // Get table schema to know which columns to search in
            DataTable schemaTable = await this.GetTableSchemaAsync(tableName);

            // Add search term condition
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                List<string> searchConditions = new();

                foreach (DataRow row in schemaTable.Rows)
                {
                    string columnName = row["name"].ToString() ?? string.Empty;
                    string columnType = row["type"].ToString()?.ToLower() ?? string.Empty;

                    // Only search in text columns
                    if (columnType.Contains("text") || columnType.Contains("char") || columnType.Contains("clob"))
                    {
                        searchConditions.Add($"{columnName} LIKE @SearchTerm");
                    }
                    // Search in numeric columns if the search term is a number
                    else if ((columnType.Contains("int") || columnType.Contains("real") || columnType.Contains("double")) && double.TryParse(searchTerm, out _))
                    {
                        searchConditions.Add($"{columnName} = @SearchTermNumeric");
                    }
                }

                if (searchConditions.Count > 0)
                {
                    conditions.Add($"({string.Join(" OR ", searchConditions)})");
                    parameters.Add(new SQLiteParameter("@SearchTerm", $"%{searchTerm}%"));

                    if (double.TryParse(searchTerm, out double numericValue))
                    {
                        parameters.Add(new SQLiteParameter("@SearchTermNumeric", numericValue));
                    }
                }
            }

            // Combine all conditions
            string whereClause = conditions.Count > 0 ? $"WHERE {string.Join(" AND ", conditions)}" : string.Empty;

            return (whereClause, parameters);
        }

        // Validate table name to prevent SQL injection
        private static bool IsValidTableName(string tableName)
        {
            return !string.IsNullOrWhiteSpace(tableName) &&
                   System.Text.RegularExpressions.Regex.IsMatch(tableName, @"^[a-zA-Z0-9_]+$");
        }

        // Validate column name to prevent SQL injection
        private static bool IsValidColumnName(string columnName)
        {
            return !string.IsNullOrWhiteSpace(columnName) &&
                   System.Text.RegularExpressions.Regex.IsMatch(columnName, @"^[a-zA-Z0-9_]+$");
        }
    }
}