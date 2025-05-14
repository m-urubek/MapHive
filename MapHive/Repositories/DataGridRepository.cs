namespace MapHive.Repositories
{
    using System.Data;
    using System.Data.SQLite;
    using System.Text.RegularExpressions;
    using MapHive.Models.BusinessModels;
    using MapHive.Models.RepositoryModels;
    using MapHive.Services;
    using MapHive.Singletons;
    using MapHive.Utilities.Extensions;

    public partial class DataGridRepository(ISqlClientSingleton sqlClientSingleton, ILogManagerService logManagerService) : IDataGridRepository
    {
        private readonly ISqlClientSingleton _sqlClientSingleton = sqlClientSingleton;
        private readonly ILogManagerService _logManagerService = logManagerService;

        // Get schema information for a table
        public async Task<DataTable> GetTableSchemaAsync(string tableName)
        {
            // Sanitize table name to prevent SQL injection
            if (!IsValidTableName(tableName: tableName))
            {
                throw new ArgumentException("Invalid table name");
            }

            string query = $"PRAGMA table_info({tableName})";
            return await _sqlClientSingleton.SelectAsync(query: query);
        }

        // Get schema information about foreign keys for a table
        private async Task<DataTable> GetForeignKeysAsync(string tableName)
        {
            // Sanitize table name to prevent SQL injection
            if (!IsValidTableName(tableName: tableName))
            {
                throw new ArgumentException("Invalid table name");
            }

            string query = $"PRAGMA foreign_key_list({tableName})";
            return await _sqlClientSingleton.SelectAsync(query: query);
        }

        // Get columns for a table
        public async Task<List<DataGridColumnGet>> GetColumnsForTableAsync(string tableName)
        {
            DataTable schemaTable = await GetTableSchemaAsync(tableName: tableName);
            DataTable foreignKeysTable = await GetForeignKeysAsync(tableName: tableName);

            List<DataGridColumnGet> columns = new();

            for (int i = 0; i < schemaTable.Rows.Count; i++)
            {
                DataRow row = schemaTable.Rows[i];
                string columnName = row["name"].ToString() ?? string.Empty;

                DataGridColumnGet column = new()
                {
                    DisplayName = columnName, //translate later
                    InternalName = columnName,
                    Flex = "1",      // Use shorthand flex:1 (equivalent to 1 1 0) for consistent zero-basis flex
                    Index = i,
                    IsLastColumn = false
                };

                // Check if this column is a foreign key
                bool isForeignKey = foreignKeysTable.AsEnumerable()
                                        .Any(predicate: fkRow => fkRow["from"].ToString() == columnName);

                columns.Add(item: column);
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
            if (!IsValidTableName(tableName: tableName) || !IsValidColumnName(columnName: columnName))
            {
                throw new ArgumentException("Invalid table or column name");
            }

            ColumnInfo searchInfo = new()
            {
                TableName = tableName,
                ColumnName = columnName
            };

            // Check if this column is a foreign key
            DataTable foreignKeysTable = await GetForeignKeysAsync(tableName: tableName);
            DataRow? fkRow = foreignKeysTable.AsEnumerable()
                                .FirstOrDefault(predicate: row => row["from"].ToString() == columnName);

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
            string searchColumnName = "",
            string searchTerm = "",
            string sortColumnName = "",
            string sortDirection = "asc")
        {
            // Validate parameters
            if (!IsValidTableName(tableName: tableName))
            {
                throw new ArgumentException("Invalid table name");
            }

            if (!string.IsNullOrEmpty(value: sortColumnName) && !IsValidColumnName(columnName: sortColumnName))
            {
                throw new ArgumentException("Invalid sort field");
            }

            // Ensure page and pageSize are valid
            page = Math.Max(val1: 1, val2: page);
            pageSize = Math.Clamp(value: pageSize, min: 1, max: 100); // Example clamp

            // Initialize view model
            DataGridGet dataGridGet = new()
            {
                TableName = tableName,
                CurrentPage = page,
                PageSize = pageSize,
                SearchTerm = searchTerm,
                SortField = sortColumnName,
                SortDirection = sortDirection.Equals("desc", StringComparison.InvariantCultureIgnoreCase) ? "desc" : "asc", // Sanitize direction
                // Get table columns
                Columns = await GetColumnsForTableAsync(tableName: tableName),
                Items = new List<DataGridRowGet>(),
                TotalCount = 0
            };

            // Build query
            (string query, List<SQLiteParameter> parameters) = await BuildQueryAsync(
                tableName: tableName,
                columns: dataGridGet.Columns, // Pass columns to avoid re-fetching
                page: page,
                pageSize: pageSize,
                searchColumnName: searchColumnName,
                searchTerm: searchTerm,
                sortColumnName: sortColumnName,
                sortDirection: dataGridGet.SortDirection
            );

            // Execute query to get data
            DataTable dataTable = await _sqlClientSingleton.SelectAsync(query: query, parameters: [.. parameters]);

            // Convert data to grid rows
            dataGridGet.Items = ConvertDataTableToGridRows(dataTable: dataTable, columns: dataGridGet.Columns, tableName: tableName);

            // Get total count for pagination
            dataGridGet.TotalCount = await GetTotalRowsCountAsync(tableName: tableName, searchColumnName: sortColumnName, searchTerm: searchTerm);

            return dataGridGet;
        }

        // Helper method to convert DataTable to grid rows
        private List<DataGridRowGet> ConvertDataTableToGridRows(DataTable dataTable, List<DataGridColumnGet> columns, string tableName)
        {
            List<DataGridRowGet> rows = new();
            string idColumnName = columns.FirstOrDefault(predicate: c => c.InternalName.StartsWith(value: "Id_"))?.InternalName ?? throw new Exception($"{nameof(ConvertDataTableToGridRows)}: idColumnName is null!");

            foreach (DataRow dataRow in dataTable.Rows)
            {
                // Assign RowId using mapping extension
                int rowId = dataRow.GetValueOrThrow<int>(columnName: idColumnName);

                Dictionary<string, DataGridCellGet> cellsByColumnNames = new();
                foreach (DataGridColumnGet column in columns)
                {
                    string columnName = column.InternalName;
                    if (dataTable.Columns.Contains(name: columnName))
                    {
                        string cellContent = dataRow.ToNullableString(columnName: columnName) ?? string.Empty;
                        cellsByColumnNames.Add(columnName, new DataGridCellGet { Content = cellContent });
                    }
                    else
                    {
                        // Handle cases where a column might be expected but not in the result (e.g., calculated columns)
                        cellsByColumnNames.Add(columnName, new DataGridCellGet { Content = string.Empty }); // Or handle differently
                        _ = _logManagerService.LogAsync(Models.Enums.LogSeverity.Error, $"Column \"{columnName}\" not found in the result set for table \"{tableName}\".");
                    }
                }
                rows.Add(item: new()
                {
                    RowId = rowId,
                    CellsByColumnNames = cellsByColumnNames
                });
            }
            return rows;
        }

        // Get total count of rows matching the search criteria
        public async Task<int> GetTotalRowsCountAsync(string tableName, string searchColumnName, string searchTerm)
        {
            // Validate
            if (!IsValidTableName(tableName: tableName))
            {
                throw new ArgumentException("Invalid table name");
            }

            // Build WHERE clause for counting
            (string whereClause, List<SQLiteParameter> parameters) = BuildWhereClause(searchColumnName: searchColumnName, searchTerm: searchTerm);

            string query = $"SELECT COUNT(*) FROM {tableName} {whereClause}";

            // Execute query
            DataTable resultTable = await _sqlClientSingleton.SelectAsync(query: query, parameters: [.. parameters]);

            return resultTable.Rows.Count > 0 && resultTable.Rows[0][0] != DBNull.Value ? Convert.ToInt32(value: resultTable.Rows[0][0]) : 0;
        }

        // Build the main SQL query for fetching grid data
        private async Task<(string query, List<SQLiteParameter> parameters)> BuildQueryAsync(
            string tableName,
            List<DataGridColumnGet> columns,
            int page,
            int pageSize,
            string searchColumnName,
            string searchTerm,
            string sortColumnName,
            string sortDirection)
        {
            // Build WHERE clause
            (string whereClause, List<SQLiteParameter> parameters) = BuildWhereClause(searchColumnName: searchColumnName, searchTerm: searchTerm);

            // Build ORDER BY clause
            string orderByClause = string.Empty;
            if (!string.IsNullOrEmpty(value: sortColumnName) && IsValidColumnName(columnName: sortColumnName) && columns.Any(predicate: c => c.InternalName == sortColumnName))
            {
                string direction = sortDirection.Equals(value: "desc", comparisonType: StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
                orderByClause = $"ORDER BY \"{sortColumnName}\" {direction}"; // Quote column name
            }
            else
            {
                // Default sort by the first column (usually ID) if available
                string? firstColumn = columns.FirstOrDefault(predicate: c => c.InternalName.StartsWith(value: "Id_"))?.InternalName ?? columns.FirstOrDefault()?.InternalName;
                if (!string.IsNullOrEmpty(value: firstColumn))
                {
                    orderByClause = $"ORDER BY \"{firstColumn}\" ASC"; // Quote column name
                }
            }

            // Build LIMIT/OFFSET clause for pagination
            int offset = (page - 1) * pageSize;
            string limitClause = "LIMIT @PageSize OFFSET @Offset";
            parameters.Add(item: new SQLiteParameter("@PageSize", pageSize));
            parameters.Add(item: new SQLiteParameter("@Offset", offset));

            // Select only the columns needed for the grid
            string selectColumns = string.Join(separator: ", ", values: columns.Select(selector: c => $"\"{c.InternalName}\"")); // Quote column names
            if (!columns.Any(predicate: c => c.InternalName.StartsWith(value: "Id_"))) // Ensure ID column is selected if not already included
            {
                DataRow? idCol = (await GetTableSchemaAsync(tableName: tableName)).AsEnumerable().FirstOrDefault(predicate: r => ((string)r["name"]).StartsWith(value: "Id_"));
                if (idCol != null && !selectColumns.Contains(value: $"\"{idCol["name"]}\""))
                {
                    selectColumns = $"\"{idCol["name"]}\", {selectColumns}";
                }
            }
            if (string.IsNullOrEmpty(value: selectColumns))
            {
                selectColumns = "*"; // Fallback if no columns somehow
            }

            string query = $"SELECT {selectColumns} FROM {tableName} {whereClause} {orderByClause} {limitClause}";

            return (query, parameters);
        }

        // Build the WHERE clause based on the search term
        private static (string whereClause, List<SQLiteParameter> parameters) BuildWhereClause(
            string searchColumnName,
            string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(value: searchTerm))
            {
                return (string.Empty, new List<SQLiteParameter>());
            }

            List<string> conditions = new();
            List<SQLiteParameter> parameters = new();
            int paramIndex = 0;

            // Maybe add type checking here later to avoid searching non-text fields?
            string paramName = $"@SearchParam{paramIndex++}";
            conditions.Add(item: $"CAST(\"{searchColumnName}\" AS TEXT) LIKE {paramName}"); // Quote column name
            parameters.Add(item: new SQLiteParameter(paramName, $"%{searchTerm}%"));

            if (conditions.Count != 0)
            {
                return ("WHERE " + string.Join(separator: " OR ", values: conditions), parameters);
            }
            else
            {
                return (string.Empty, parameters); // No searchable columns found
            }
        }

        // Validate table name to prevent SQL injection
        private static bool IsValidTableName(string tableName)
        {
            return !string.IsNullOrWhiteSpace(value: tableName) && MyRegex().IsMatch(input: tableName);
        }

        // Validate column name to prevent SQL injection
        private static bool IsValidColumnName(string columnName)
        {
            return !string.IsNullOrWhiteSpace(value: columnName) && MyRegex1().IsMatch(input: columnName);
        }

        [GeneratedRegex(@"^[a-zA-Z0-9_]+$")]
        private static partial Regex MyRegex();
        [GeneratedRegex(@"^[a-zA-Z0-9_]+$")]
        private static partial Regex MyRegex1();
    }
}
