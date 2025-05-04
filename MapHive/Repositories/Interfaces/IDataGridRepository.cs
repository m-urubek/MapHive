using MapHive.Models.BusinessModels;
using MapHive.Models.RepositoryModels;
using System.Data;

namespace MapHive.Repositories
{
    public interface IDataGridRepository
    {
        Task<DataTable> GetTableSchemaAsync(string tableName);
        Task<List<DataGridColumnGet>> GetColumnsForTableAsync(string tableName);
        Task<ColumnInfo> GetColumnInfoAsync(string tableName, string columnName);
        Task<DataGridGet> GetGridDataAsync(string tableName, int page = 1, int pageSize = 20,
            string searchTerm = "", string sortField = "", string sortDirection = "asc");
        Task<int> GetTotalRowsCountAsync(string tableName, string searchTerm, List<DataGridColumnGet> columns);
    }
}