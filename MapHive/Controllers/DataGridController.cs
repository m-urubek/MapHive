namespace MapHive.Controllers
{
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using MapHive.Models.BusinessModels;
    using MapHive.Models.RepositoryModels;
    using MapHive.Services;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    [Authorize(Roles = "Admin,2")]
    public class DataGridController : Controller
    {
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly IDataGridService _dataGridService;

        public DataGridController(IDataGridService dataGridService)
        {
            _dataGridService = dataGridService;
            _jsonOptions = new JsonSerializerOptions
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
            string searchColumn = "",
            string searchTerm = "",
            string sortColumnName = "",
            string sortDirection = "asc")
        {
            DataGridGet dataGridGet = await _dataGridService.GetGridDataAsync(tableName: tableName, page: page, searchColumn: searchColumn, searchTerm: searchTerm, sortColumnName: sortColumnName, sortDirection: sortDirection);
            return Json(data: dataGridGet, serializerSettings: _jsonOptions);
        }

        /// <summary>
        /// Get column info for a table
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetColumnInfo(string tableName, string columnName)
        {
            ColumnInfo info = await _dataGridService.GetColumnInfoAsync(tableName: tableName, columnName: columnName);
            return Json(data: new { success = true, columnInfo = info }, serializerSettings: _jsonOptions);
        }

        /// <summary>
        /// Get all columns for a table
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetColumns(string tableName)
        {
            List<DataGridColumnGet> columns = await _dataGridService.GetColumnsForTableAsync(tableName: tableName);
            return Json(data: new { success = true, columns }, serializerSettings: _jsonOptions);
        }
    }
}