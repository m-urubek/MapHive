namespace MapHive.Services
{
    using MapHive.Models.BusinessModels;
    using MapHive.Models.RepositoryModels;
    using MapHive.Models.ViewModels;

    public interface IDataGridService
    {
        Task<DataGridViewModel> GetGridDataAsync(
            string tableName,
            int page = 1,
            string searchTerm = "",
            string sortField = "",
            string sortDirection = "asc");

        Task<ColumnInfo> GetColumnInfoAsync(string tableName, string columnName);

        Task<List<DataGridColumnGet>> GetColumnsForTableAsync(string tableName);
    }
}