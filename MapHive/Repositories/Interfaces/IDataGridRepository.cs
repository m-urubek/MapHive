namespace MapHive.Repositories
{
    using System.Data;
    using MapHive.Models.BusinessModels;
    using MapHive.Models.RepositoryModels;

    public interface IDataGridRepository
    {
        Task<DataTable> GetTableSchemaAsync(string tableName);
        Task<List<DataGridColumnGet>> GetColumnsForTableAsync(string tableName);
        Task<ColumnInfo> GetColumnInfoAsync(string tableName, string columnName);
        Task<DataGridGet> GetGridDataAsync(string tableName, int page = 1, int pageSize = 20, string searchColumn = "",
            string searchTerm = "", string sortColumnName = "", string sortDirection = "asc");
        Task<int> GetTotalRowsCountAsync(string tableName, string searchColumnName, string searchTerm);
    }
}