using MapHive.Models.BusinessModels;
using MapHive.Models.RepositoryModels;
using MapHive.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MapHive.Controllers
{
    public class DataGridController : Controller
    {
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly IDataGridService _dataGridService;

        public DataGridController(IDataGridService dataGridService)
        {
            this._dataGridService = dataGridService;
            this._jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
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
            Models.ViewModels.DataGridViewModel vm = await this._dataGridService.GetGridDataAsync(tableName, page, searchTerm, sortField, sortDirection);
            return this.Json(new
            {
                success = true,
                totalPages = vm.TotalPages,
                currentPage = vm.CurrentPage,
                totalCount = vm.TotalCount,
                items = vm.Items,
                columns = vm.Columns,
                tableName = vm.TableName,
                sortField = vm.SortField,
                sortDirection = vm.SortDirection,
                searchTerm = vm.SearchTerm,
                searchColumn = vm.SearchColumn
            }, this._jsonOptions);
        }

        /// <summary>
        /// Get column info for a table
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetColumnInfo(string tableName, string columnName)
        {
            ColumnInfo info = await this._dataGridService.GetColumnInfoAsync(tableName, columnName);
            return this.Json(new { success = true, columnInfo = info }, this._jsonOptions);
        }

        /// <summary>
        /// Get all columns for a table
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetColumns(string tableName)
        {
            List<DataGridColumnGet> columns = await this._dataGridService.GetColumnsForTableAsync(tableName);
            return this.Json(new { success = true, columns }, this._jsonOptions);
        }
    }
}