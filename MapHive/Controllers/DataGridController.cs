namespace MapHive.Controllers;

using System.Text.Json;
using System.Text.Json.Serialization;
using MapHive.Models.Data.DataGrid;
using MapHive.Models.PageModels;
using MapHive.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize(Roles = "Admin,2")]
public class DataGridController(IDataGridRepository _dataGridRepository) : Controller
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [HttpGet("DataGrid/{tableName:required}")]
    public async Task<IActionResult> Index(string tableName)
    {
        DataGridPageModel pageModel = new()
        {
            Title = tableName,
            ColumnNames = new List<string>(),
            Columns = await _dataGridRepository.GetColumnsForTableAsync(tableName: tableName)
        };
        return View("DataGrid", pageModel);
    }

    /// <summary>
    /// AJAX endpoint to get grid data
    /// </summary>
    [HttpGet("GetGridData/{tableName:required}")]
    public async Task<IActionResult> GetGridData(
        string tableName,
        int page = 1,
        string searchColumn = "",
        string searchTerm = "",
        string sortColumnName = "",
        string sortDirection = "asc")
    {
        DataGrid dataGrid = await _dataGridRepository.GetGridDataAsync(
            tableName: tableName,
            page: page,
            searchTerm: searchTerm,
            sortColumnName: sortColumnName,
            ascending: sortDirection == "asc",
            searchColumnName: searchColumn
        );
        return Json(data: dataGrid, serializerSettings: _jsonOptions);
    }

    /// <summary>
    /// Get column info for a table
    /// </summary>
    [HttpGet("GetColumnInfo/{tableName:required}/{columnName:required}")]
    public async Task<IActionResult> GetColumnInfo(string tableName, string columnName)
    {
        ColumnInfo info = await _dataGridRepository.GetColumnInfoAsync(tableName: tableName, columnName: columnName);
        return Json(data: new { success = true, columnInfo = info }, serializerSettings: _jsonOptions);
    }

    /// <summary>
    /// Get all columns for a table
    /// </summary>
    [HttpGet("GetColumns/{tableName:required}")]
    public async Task<IActionResult> GetColumns(string tableName)
    {
        List<DataGridColumn> columns = await _dataGridRepository.GetColumnsForTableAsync(tableName: tableName);
        return Json(data: new { success = true, columns }, serializerSettings: _jsonOptions);
    }
}
