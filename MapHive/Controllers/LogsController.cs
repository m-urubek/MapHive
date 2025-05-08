using MapHive.Models.ViewModels;
using MapHive.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MapHive.Controllers
{
    [Authorize]
    public class LogsController : Controller
    {
        private readonly IDataGridService _dataGridService;

        public LogsController(IDataGridService dataGridService)
        {
            this._dataGridService = dataGridService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> Index(
            string searchTerm = "",
            int page = 1,
            int pageSize = 20,
            string sortField = "Timestamp",
            string sortDirection = "desc")
        {
            DataGridViewModel viewModel = await this._dataGridService.GetGridDataAsync(
                tableName: "Logs",
                page: page,
                searchTerm: searchTerm,
                sortField: sortField,
                sortDirection: sortDirection);
            return this.View(viewModel);
        }
    }
}