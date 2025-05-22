namespace MapHive.Repositories;

using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text.RegularExpressions;
using MapHive.Models.Data.DataGrid;
using MapHive.Services;
using MapHive.Singletons;

public partial class DataGridRepository(ISqlClientSingleton sqlClientSingleton, ILogManagerService logManagerService) : IDataGridRepository
{
    private readonly ISqlClientSingleton _sqlClientSingleton = sqlClientSingleton;
    private readonly ILogManagerService _logManagerService = logManagerService;

    private readonly Dictionary<string, string> tablesDisplayColumns = new()
    {
        { "Accounts", "Username" },
        { "Categories", "Name" },
        { "Configuration", "Key" },
        { "DiscussionThreads", "ThreadName" },
        { "Logs", "Message" },
        { "MapLocations", "Name" },
        { "Reviews", "ReviewText" },
        { "ThreadMessages", "MessageText" },
        { "IpBans", "HashedIpAddress" },
        { "AccountBans", "Reason" }
    };

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
    public async Task<List<DataGridColumn>> GetColumnsForTableAsync(string tableName)
    {
        DataTable schemaTable = await GetTableSchemaAsync(tableName: tableName);
        DataTable foreignKeys = await GetForeignKeysAsync(tableName: tableName);

        List<DataGridColumn> columns = new();

        for (int i = 0; i < schemaTable.Rows.Count; i++)
        {
            DataRow row = schemaTable.Rows[i];
            string columnName = row["name"].ToString() ?? string.Empty;

            // Prefix each column name with table name
            string baseInternalName = $"{tableName}.{columnName}";
            DataGridColumn column = new()
            {
                TableName = tableName,
                DisplayName = baseInternalName,
                InternalName = baseInternalName,
                Flex = "1",
                Index = (i * 2) - 1,
                IsLastColumn = false,
                LinkedForeignKey = baseInternalName,
                JoinAlias = null
            };

            // Add display column for foreign key if applicable
            DataRow? foreignKey = foreignKeys.AsEnumerable().FirstOrDefault(fkRow => fkRow["from"].ToString() == columnName);
            if (foreignKey != null)
            {
                string foreignTable = foreignKey["table"].ToString()!;
                string displayCol = tablesDisplayColumns[foreignTable];
                // Unique internal name for display column: include display field
                string displayInternalName = $"{tableName}.{columnName}.{displayCol}";
                // Add a display column mapping foreign key to its lookup value
                columns.Add(new DataGridColumn
                {
                    TableName = foreignTable,
                    DisplayName = displayCol,
                    InternalName = displayInternalName,
                    Flex = "1",
                    Index = i * 2,
                    IsLastColumn = false,
                    LinkedForeignKey = $"{foreignTable}.{displayCol}",
                    JoinAlias = columnName // alias = foreign key column name
                });
            }

            columns.Add(column);
        }

        // Remove prefixes from display names except where duplicates exist
        IEnumerable<string> plainNames = columns.Select(c => c.InternalName.Split('.')[1]);
        HashSet<string> duplicates = [.. plainNames.GroupBy(n => n).Where(g => g.Count() > 1).Select(g => g.Key)];
        foreach (DataGridColumn col in columns)
        {
            string[] parts = col.InternalName.Split('.');
            string plain = parts[1];
            col.DisplayName = duplicates.Contains(plain) ? col.InternalName : plain;
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

        // Check if this column is a foreign key first
        DataTable foreignKeysTable = await GetForeignKeysAsync(tableName: tableName);
        DataRow? fkRow = foreignKeysTable.AsEnumerable()
                            .FirstOrDefault(predicate: row => row["from"].ToString() == columnName);

        // Create and return the fully initialized object
        return new ColumnInfo
        {
            TableName = tableName,
            ColumnName = columnName,
            IsForeignKey = fkRow != null,
            ForeignTable = fkRow?["table"]?.ToString(),
            ForeignColumn = fkRow?["to"]?.ToString()
        };
    }

    // Get grid data with pagination, sorting, and searching
    public async Task<DataGrid> GetGridDataAsync(
        string tableName,
        int page,
        string searchColumnName,
        string searchTerm,
        string sortColumnName,
        bool ascending,
        int pageSize = 20)
    {
        // Ensure page and pageSize are valid
        page = Math.Max(val1: 1, val2: page);
        pageSize = Math.Clamp(value: pageSize, min: 1, max: 100); // Example clamp

        List<DataGridColumn> columns = await GetColumnsForTableAsync(tableName: tableName);

        // Build query
        (string query, List<SQLiteParameter> parameters) = await BuildQueryAsync(
            tableName: tableName,
            columns: columns, // Pass columns to avoid re-fetching
            page: page,
            pageSize: pageSize,
            searchColumnName: searchColumnName,
            searchTerm: searchTerm,
            sortColumnName: sortColumnName,
            ascending: ascending
        );

        // Execute query to get data
        DataTable dataTable = await _sqlClientSingleton.SelectAsync(query: query, parameters: [.. parameters]);

        // Convert data to grid rows
        List<DataGridRow> items = ConvertDataTableToGridRows(dataTable: dataTable, columns: columns, tableName: tableName);

        // Get total count for pagination
        int totalCount = await GetTotalRowsCountAsync(tableName: tableName, searchColumnName: sortColumnName, searchTerm: searchTerm);

        // Determine effective sort for UI if none provided
        string effectiveSortField = sortColumnName;
        bool effectiveAscending = ascending;
        if (string.IsNullOrEmpty(sortColumnName))
        {
            // Default to primary-key column descending
            string defaultId = columns.First(c => c.InternalName.Split('.')[1].StartsWith("Id_")).InternalName;
            effectiveSortField = defaultId;
            effectiveAscending = false;
        }
        return new()
        {
            TableName = tableName,
            CurrentPage = page,
            PageSize = pageSize,
            SearchTerm = searchTerm,
            SearchColumn = searchColumnName,
            SortField = effectiveSortField,
            Ascending = effectiveAscending,
            Columns = columns,
            Items = items,
            TotalCount = totalCount
        };
    }

    // Helper method to convert DataTable to grid rows
    private List<DataGridRow> ConvertDataTableToGridRows(DataTable dataTable, List<DataGridColumn> columns, string tableName)
    {
        // Debug: log actual DataTable column names
        string dtColumnNames = string.Join(", ", dataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
        List<DataGridRow> rows = new();
        // Determine the fully-qualified ID column (TableName.Id_*)
        string idColumnName =
            (columns.FirstOrDefault(column => column.InternalName.Split('.')[1].StartsWith("Id_"))
            ?? throw new Exception(nameof(ConvertDataTableToGridRows) + ": No ID column provided"))
            .InternalName;

        foreach (DataRow dataRow in dataTable.Rows)
        {
            // Assign RowId using mapping extension
            int rowId = dataRow.GetValueThrowNotPresentOrNull<int>(columnName: idColumnName);

            Dictionary<string, string> cellsByColumnNames = new();
            foreach (DataGridColumn column in columns)
            {
                string columnName = column.InternalName;
                if (dataTable.Columns.Contains(name: columnName))
                {
                    string cellContent = string.Empty;
                    object? cellValue = dataRow.GetValueThrowNotPresent<object?>(columnName: columnName);
                    if (cellValue is not null && cellValue != DBNull.Value)
                        cellContent = cellValue is DateTime valueAsDateTime ? valueAsDateTime.ToString("g") : cellValue.ToString() ?? string.Empty;
                    cellsByColumnNames.Add(key: columnName, value: cellContent);
                }
                else
                {
                    // Handle cases where a column might be expected but not in the result (e.g., calculated columns)
                    cellsByColumnNames.Add(key: columnName, value: string.Empty); // Or handle differently
                    _ = _logManagerService.LogAsync(Models.Enums.LogSeverity.Error, $"Column \"{columnName}\" not found in the result set for table \"{tableName}\".");
                }
            }
            rows.Add(item: new()
            {
                RowId = rowId,
                ValuesByColumnNames = cellsByColumnNames
            });
        }
        return rows;
    }

    // Get total count of rows matching the search criteria
    public async Task<int> GetTotalRowsCountAsync(string tableName, string searchColumnName, string searchTerm)
    {
        if (!IsValidTableName(tableName: tableName))
        {
            throw new ArgumentException("Invalid table name");
        }

        // Build JOIN clauses
        DataTable fks = await GetForeignKeysAsync(tableName: tableName);

        // WHERE clause
        List<SQLiteParameter> parameters = new();
        string whereClause = string.Empty;
        if (!string.IsNullOrWhiteSpace(searchColumnName) && !string.IsNullOrWhiteSpace(searchTerm))
        {
            List<DataGridColumn> columns = await GetColumnsForTableAsync(tableName: tableName);
            DataGridColumn? searchCol = columns.FirstOrDefault(c => c.InternalName == searchColumnName);
            string tbl;
            string col;
            if (searchCol != null && !string.IsNullOrEmpty(searchCol.JoinAlias))
            {
                tbl = searchCol.JoinAlias;
                string[] parts = searchCol.InternalName.Split('.');
                col = parts[^1];
            }
            else
            {
                string linked = searchCol?.LinkedForeignKey ?? searchColumnName;
                string[] parts = linked.Split('.');
                tbl = parts[0];
                col = parts[1];
            }
            SQLiteParameter param = new("@SearchParam", $"%{searchTerm}%");
            parameters.Add(param);
            whereClause = $"WHERE CAST(\"{tbl}\".\"{col}\" AS TEXT) LIKE @SearchParam";
        }

        string query = $"SELECT COUNT(*) FROM \"{tableName}\" {string.Join(" ", (List<string?>)[.. fks.AsEnumerable()
            .Select(fkRow =>
            {
                string? fkTable = fkRow["table"].ToString();
                string? fkFrom = fkRow["from"].ToString();
                string? fkTo = fkRow["to"].ToString();
                if (string.IsNullOrEmpty(fkTable) || string.IsNullOrEmpty(fkFrom) || string.IsNullOrEmpty(fkTo))
                    return null;
                // Alias the join to avoid ambiguity when the same table is joined multiple times
                return $"LEFT JOIN \"{fkTable}\" AS \"{fkFrom}\" ON \"{tableName}\".\"{fkFrom}\" = \"{fkFrom}\".\"{fkTo}\"";
            })
            .Where(j => !string.IsNullOrEmpty(j))])} {whereClause}";
        DataTable resultTable = await _sqlClientSingleton.SelectAsync(query: query, parameters: [.. parameters]);

        return resultTable.Rows.Count > 0 && resultTable.Rows[0][0] != DBNull.Value
            ? Convert.ToInt32(value: resultTable.Rows[0][0])
            : 0;
    }

    // Build the main SQL query for fetching grid data
    private async Task<(string query, List<SQLiteParameter> parameters)> BuildQueryAsync(
        string tableName,
        List<DataGridColumn> columns,
        int page,
        int pageSize,
        string searchColumnName,
        string searchTerm,
        string sortColumnName,
        bool ascending)
    {
        // Build JOIN clauses for foreign keys
        DataTable fks = await GetForeignKeysAsync(tableName: tableName);
        string joins = string.Join(" ", (List<string?>)[.. fks.AsEnumerable()
            .Select(fkRow =>
            {
                string? fkTable = fkRow["table"].ToString();
                string? fkFrom = fkRow["from"].ToString();
                string? fkTo = fkRow["to"].ToString();
                if (string.IsNullOrEmpty(fkTable) || string.IsNullOrEmpty(fkFrom) || string.IsNullOrEmpty(fkTo))
                    return null;
                // Alias the table to the foreign-key column name for this join
                return $"LEFT JOIN \"{fkTable}\" AS \"{fkFrom}\" ON \"{tableName}\".\"{fkFrom}\" = \"{fkFrom}\".\"{fkTo}\"";
            })
            .Where(j => !string.IsNullOrEmpty(j))]);

        // WHERE clause
        List<SQLiteParameter> parameters = new();
        string whereClause = string.Empty;
        if (!string.IsNullOrWhiteSpace(searchColumnName) && !string.IsNullOrWhiteSpace(searchTerm))
        {
            DataGridColumn? searchCol = columns.FirstOrDefault(c => c.InternalName == searchColumnName);
            string tbl;
            string col;
            if (searchCol != null && !string.IsNullOrEmpty(searchCol.JoinAlias))
            {
                tbl = searchCol.JoinAlias;
                string[] parts = searchCol.InternalName.Split('.');
                col = parts[^1];
            }
            else
            {
                string linked = searchCol?.LinkedForeignKey ?? searchColumnName;
                string[] parts = linked.Split('.');
                tbl = parts[0];
                col = parts[1];
            }
            SQLiteParameter param = new("@SearchParam", $"%{searchTerm}%");
            parameters.Add(param);
            whereClause = $"WHERE CAST(\"{tbl}\".\"{col}\" AS TEXT) LIKE @SearchParam";
        }

        // ORDER BY clause
        string orderByClause = string.Empty;
        if (!string.IsNullOrEmpty(sortColumnName) && columns.Any(c => c.InternalName == sortColumnName))
        {
            DataGridColumn sortCol = columns.First(c => c.InternalName == sortColumnName);
            string dir = ascending ? "ASC" : "DESC";
            if (!string.IsNullOrEmpty(sortCol.JoinAlias))
            {
                // Use join alias and display field for foreign-key columns
                string[] parts = sortCol.InternalName.Split('.');
                string displayField = parts[^1];
                orderByClause = $"ORDER BY \"{sortCol.JoinAlias}\".\"{displayField}\" {dir}";
            }
            else
            {
                // Base table column
                string[] parts = sortCol.InternalName.Split('.');
                orderByClause = $"ORDER BY \"{parts[0]}\".\"{parts[1]}\" {dir}";
            }
        }
        else
        {
            // Default sort by primary key column descending
            DataGridColumn? idCol = columns.FirstOrDefault(c => c.InternalName.Split('.')[1].StartsWith("Id_"));
            if (idCol != null)
            {
                string[] parts = idCol.InternalName.Split('.');
                orderByClause = $"ORDER BY \"{parts[0]}\".\"{parts[1]}\" DESC";
            }
        }

        // LIMIT/OFFSET
        int offset = (page - 1) * pageSize;
        parameters.Add(new SQLiteParameter("@PageSize", pageSize));
        parameters.Add(new SQLiteParameter("@Offset", offset));
        string limitClause = "LIMIT @PageSize OFFSET @Offset";

        // SELECT columns with aliases
        string selectColumns = string.Join(", ", columns.Select(c =>
        {
            // If this column came from a foreign-key join, use its join alias and display field
            if (!string.IsNullOrEmpty(c.JoinAlias))
            {
                // The InternalName parts: [TableName, ForeignKeyColumn, DisplayCol]
                string[] parts = c.InternalName.Split('.');
                string displayField = parts[^1];
                string aliasName = c.JoinAlias;
                return $"\"{aliasName}\".\"{displayField}\" AS \"{c.InternalName}\"";
            }
            // Otherwise select directly from the base table
            string[] baseParts = c.InternalName.Split('.');
            string baseTbl = baseParts[0];
            string baseCol = baseParts[1];
            return $"\"{baseTbl}\".\"{baseCol}\" AS \"{c.InternalName}\"";
        }));

        string query = $"SELECT {selectColumns} FROM \"{tableName}\" {joins} {whereClause} {orderByClause} {limitClause}";
        return (query, parameters);
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
