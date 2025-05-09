namespace MapHive.Services
{
    using AutoMapper;
    using MapHive.Models.BusinessModels;
    using MapHive.Models.RepositoryModels;
    using MapHive.Models.ViewModels;
    using MapHive.Repositories;

    public class DataGridService(
        IDataGridRepository dataGridRepository,
        IDisplayPageRepository displayRepository,
        IMapper mapper) : IDataGridService
    {
        private readonly IDataGridRepository _dataGridRepository = dataGridRepository;
        private readonly IDisplayPageRepository _displayRepository = displayRepository;
        private readonly IMapper _mapper = mapper;

        public async Task<DataGridViewModel> GetGridDataAsync(
            string tableName,
            int page = 1,
            string searchTerm = "",
            string sortField = "",
            string sortDirection = "asc")
        {
            // Retrieve raw grid data
            DataGridGet gridDataDto = await _dataGridRepository.GetGridDataAsync(
tableName: tableName, page: page, pageSize: 20, searchTerm: searchTerm, sortField: sortField, sortDirection: sortDirection);

            // Replace foreign key display (mimic controller logic)
            foreach (DataGridColumnGet column in gridDataDto.Columns)
            {
                ColumnInfo info = await _dataGridRepository.GetColumnInfoAsync(tableName: tableName, columnName: column.InternalName);
                if (info.IsForeignKey && !string.IsNullOrEmpty(value: info.ForeignTable))
                {
                    // Only Users display column by default
                    string displayCol = info.ForeignTable.Equals(value: "Users", comparisonType: StringComparison.OrdinalIgnoreCase)
                        ? "Username" : info.ForeignColumn ?? string.Empty;

                    foreach (DataGridRowGet row in gridDataDto.Items)
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

            // Map to view model before returning
            DataGridViewModel viewModel = _mapper.Map<DataGridViewModel>(source: gridDataDto);
            return viewModel;
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