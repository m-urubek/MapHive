namespace MapHive.Services
{
    using AutoMapper;
    using MapHive.Models.BusinessModels;
    using MapHive.Models.RepositoryModels;
    using MapHive.Models.ViewModels;
    using MapHive.Repositories;

    public class DataGridService(
        IDataGridRepository dataGridRepository,
        IDisplayPageRepository displayRepository) : IDataGridService
    {
        private readonly IDataGridRepository _dataGridRepository = dataGridRepository;
        private readonly IDisplayPageRepository _displayRepository = displayRepository;

        public async Task<DataGridGet> GetGridDataAsync(
            string tableName,
            int page = 1,
            string searchTerm = "",
            string searchColumn = "",
            string sortColumnName = "",
            string sortDirection = "asc")
        {
            // Retrieve raw grid data
            DataGridGet dataGridGet = await _dataGridRepository.GetGridDataAsync(
tableName: tableName, page: page, pageSize: 20, searchColumn: searchColumn, searchTerm: searchTerm, sortColumnName: sortColumnName, sortDirection: sortDirection);

            // Replace foreign key display (mimic controller logic)
            foreach (DataGridColumnGet column in dataGridGet.Columns)
            {
                ColumnInfo info = await _dataGridRepository.GetColumnInfoAsync(tableName: tableName, columnName: column.InternalName);
                if (info.IsForeignKey && !string.IsNullOrEmpty(value: info.ForeignTable))
                {
                    // Only Users display column by default
                    string displayCol = info.ForeignTable.Equals(value: "Users", comparisonType: StringComparison.OrdinalIgnoreCase)
                        ? "Username" : info.ForeignColumn ?? string.Empty;

                    foreach (DataGridRowGet row in dataGridGet.Items)
                    {
                        if (row.CellsByColumnNames.TryGetValue(key: column.InternalName, value: out DataGridCellGet? cell)
                            && int.TryParse(s: cell.Content, result: out int fkId))
                        {
                            Dictionary<string, string> foreignData = await _displayRepository.GetItemDataAsync(tableName: info.ForeignTable!, id: fkId);
                            if (foreignData.TryGetValue(key: displayCol, value: out string? val))
                            {
                                cell.Content = val;
                            }
                        }
                    }
                }
            }

            return dataGridGet;
        }

        public Task<ColumnInfo> GetColumnInfoAsync(string tableName, string columnName)
        {
            return _dataGridRepository.GetColumnInfoAsync(tableName: tableName, columnName: columnName);
        }

        public Task<List<DataGridColumnGet>> GetColumnsForTableAsync(string tableName)
        {
            return _dataGridRepository.GetColumnsForTableAsync(tableName: tableName);
        }
    }
}