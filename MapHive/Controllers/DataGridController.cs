using AutoMapper;
using MapHive.Models.BusinessModels;
using MapHive.Models.RepositoryModels;
using MapHive.Models.ViewModels;
using MapHive.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MapHive.Controllers
{
    public class DataGridController : Controller
    {
        /// <summary>
        /// Maps foreign table names to their display column names.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string> ForeignTableDisplayColumns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Users", "Username" }
            // Add other table display columns as needed
        };

        private readonly JsonSerializerOptions _jsonOptions;
        private readonly IDataGridRepository _dataGridRepository;
        private readonly IMapper _mapper;
        private readonly IDisplayRepository _displayRepository;

        public DataGridController(IDataGridRepository dataGridRepository, IMapper mapper, IDisplayRepository displayRepository)
        {
            this._jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            this._dataGridRepository = dataGridRepository;
            this._mapper = mapper;
            this._displayRepository = displayRepository;
        }

        /// <summary>
        /// AJAX endpoint to get grid data
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetGridData(
            string tableName,
            int page = 1,
            string searchTerm = "",
            string sortField = "",
            string sortDirection = "asc")
        {
            try
            {
                // Get grid data
                DataGridViewModel viewModel = this._mapper.Map<DataGridViewModel>(
                    await this._dataGridRepository.GetGridDataAsync(
                        tableName,
                        page,
                        20, // Default page size
                        searchTerm,
                        sortField,
                        sortDirection)
                );

                // Replace foreign key values with display values
                foreach (DataGridColumnGet column in viewModel.Columns)
                {
                    ColumnInfo columnInfo = await this._dataGridRepository.GetColumnInfoAsync(tableName, column.InternalName);
                    if (columnInfo.IsForeignKey && !string.IsNullOrEmpty(columnInfo.ForeignTable))
                    {
                        if (ForeignTableDisplayColumns.TryGetValue(columnInfo.ForeignTable, out string? displayColumn))
                        {
                            foreach (DataGridRowGet item in viewModel.Items)
                            {
                                if (item.CellsByColumnNames.TryGetValue(column.InternalName, out DataGridCellGet? cell))
                                {
                                    if (int.TryParse(cell.Content, out int fkId))
                                    {
                                        Dictionary<string, string> foreignData = await this._displayRepository.GetItemDataAsync(columnInfo.ForeignTable, fkId);
                                        if (foreignData.TryGetValue(displayColumn, out string? displayValue))
                                        {
                                            cell.Content = displayValue;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // Return grid data as JSON with proper serialization options
                return this.Json(new
                {
                    success = true,
                    totalPages = viewModel.TotalPages,
                    currentPage = page,
                    totalCount = viewModel.TotalCount,
                    items = viewModel.Items,
                    columns = viewModel.Columns,
                    tableName,
                    sortField = viewModel.SortField,
                    sortDirection = viewModel.SortDirection,
                    searchTerm = viewModel.SearchTerm,
                    searchColumn = viewModel.SearchColumn
                }, this._jsonOptions);
            }
            catch (Exception ex)
            {
                // Return error as JSON
                return this.Json(new { success = false, message = ex.Message }, this._jsonOptions);
            }
        }

        /// <summary>
        /// Get column info for a table
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetColumnInfo(string tableName, string columnName)
        {
            try
            {
                // Get column info
                ColumnInfo columnInfo = await this._dataGridRepository.GetColumnInfoAsync(tableName, columnName);

                // Return column info as JSON
                return this.Json(new
                {
                    success = true,
                    columnInfo
                }, this._jsonOptions);
            }
            catch (Exception ex)
            {
                // Return error as JSON
                return this.Json(new { success = false, message = ex.Message }, this._jsonOptions);
            }
        }

        /// <summary>
        /// Get all columns for a table
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetColumns(string tableName)
        {
            try
            {
                // Get columns
                List<DataGridColumnGet> columns = await this._dataGridRepository.GetColumnsForTableAsync(tableName);

                // Return columns as JSON
                return this.Json(new
                {
                    success = true,
                    columns
                }, this._jsonOptions);
            }
            catch (Exception ex)
            {
                // Return error as JSON
                return this.Json(new { success = false, message = ex.Message }, this._jsonOptions);
            }
        }
    }
}