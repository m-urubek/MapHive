using MapHive.Models.DataGrid;
using System.Data;

namespace MapHive.Repositories.Interfaces
{
    public interface IDataGridRepository
    {
        Task<DataTable> GetTableSchemaAsync(string tableName);
        Task<List<DataGridColumn>> GetColumnsForTableAsync(string tableName);
        Task<SearchColumnInfo> GetSearchColumnInfoAsync(string tableName, string columnName);
        Task<DataGrid> GetGridDataAsync(string tableName, int page = 1, int pageSize = 20,
            string searchTerm = "", string sortField = "", string sortDirection = "asc");
        Task<int> GetTotalRowsCountAsync(string tableName, string searchTerm = "");
    }
}