namespace MapHive.Repositories;

using System.Data;
using MapHive.Models.Data.DataGrid;

public interface IDataGridRepository
{
    Task<DataTable> GetTableSchemaAsync(string tableName);
    Task<List<DataGridColumn>> GetColumnsForTableAsync(string tableName);
    Task<ColumnInfo> GetColumnInfoAsync(string tableName, string columnName);
    public Task<DataGrid> GetGridDataAsync(
        string tableName,
        int page,
        string searchColumnName,
        string searchTerm,
        string sortColumnName,
        bool ascending,
        int pageSize = 20);

    Task<int> GetTotalRowsCountAsync(string tableName, string searchColumnName, string searchTerm);
}
