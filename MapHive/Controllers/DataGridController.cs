using MapHive.Models.DataGrid;
using MapHive.Repositories.Interfaces;
using MapHive.Singletons;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MapHive.Controllers
{
    public class DataGridController : Controller
    {
        private readonly JsonSerializerOptions _jsonOptions;

        public DataGridController()
        {
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
            try
            {
                // Get grid data
                DataGrid viewModel = await CurrentRequest.DataGridRepository.GetGridDataAsync(
                    tableName,
                    page,
                    20, // Default page size
                    searchTerm,
                    sortField,
                    sortDirection);

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
        public async Task<IActionResult> GetSearchColumnInfo(string tableName, string columnName)
        {
            try
            {
                // Get column info
                SearchColumnInfo columnInfo = await CurrentRequest.DataGridRepository.GetSearchColumnInfoAsync(tableName, columnName);

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
                List<DataGridColumn> columns = await CurrentRequest.DataGridRepository.GetColumnsForTableAsync(tableName);

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