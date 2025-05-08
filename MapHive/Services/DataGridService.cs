using AutoMapper;
using MapHive.Models.BusinessModels;
using MapHive.Models.RepositoryModels;
using MapHive.Models.ViewModels;
using MapHive.Repositories;

namespace MapHive.Services
{
    public class DataGridService : IDataGridService
    {
        private readonly IDataGridRepository _dataGridRepository;
        private readonly IDisplayRepository _displayRepository;
        private readonly IMapper _mapper;

        public DataGridService(
            IDataGridRepository dataGridRepository,
            IDisplayRepository displayRepository,
            IMapper mapper)
        {
            this._dataGridRepository = dataGridRepository;
            this._displayRepository = displayRepository;
            this._mapper = mapper;
        }

        public async Task<DataGridViewModel> GetGridDataAsync(
            string tableName,
            int page = 1,
            string searchTerm = "",
            string sortField = "",
            string sortDirection = "asc")
        {
            // Retrieve raw grid data
            DataGridGet gridDataDto = await this._dataGridRepository.GetGridDataAsync(
                tableName, page, 20, searchTerm, sortField, sortDirection);

            // Replace foreign key display (mimic controller logic)
            foreach (DataGridColumnGet column in gridDataDto.Columns)
            {
                ColumnInfo info = await this._dataGridRepository.GetColumnInfoAsync(tableName, column.InternalName);
                if (info.IsForeignKey && !string.IsNullOrEmpty(info.ForeignTable))
                {
                    // Only Users display column by default
                    string displayCol = info.ForeignTable.Equals("Users", StringComparison.OrdinalIgnoreCase)
                        ? "Username" : info.ForeignColumn ?? string.Empty;

                    foreach (DataGridRowGet row in gridDataDto.Items)
                    {
                        if (row.CellsByColumnNames.TryGetValue(column.InternalName, out DataGridCellGet? cell)
                            && int.TryParse(cell.Content, out int fkId))
                        {
                            Dictionary<string, string> foreignData = await this._displayRepository.GetItemDataAsync(info.ForeignTable!, fkId);
                            if (foreignData.TryGetValue(displayCol, out string? val))
                            {
                                cell.Content = val;
                            }
                        }
                    }
                }
            }

            // Map to view model before returning
            DataGridViewModel viewModel = this._mapper.Map<DataGridViewModel>(gridDataDto);
            return viewModel;
        }

        public Task<ColumnInfo> GetColumnInfoAsync(string tableName, string columnName)
        {
            return this._dataGridRepository.GetColumnInfoAsync(tableName, columnName);
        }

        public Task<List<DataGridColumnGet>> GetColumnsForTableAsync(string tableName)
        {
            return this._dataGridRepository.GetColumnsForTableAsync(tableName);
        }
    }
}