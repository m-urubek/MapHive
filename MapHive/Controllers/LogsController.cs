namespace MapHive.Controllers
{
    using MapHive.Models.ViewModels;
    using MapHive.Services;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    [Authorize]
    public class LogsController(IDataGridService dataGridService) : Controller
    {
        private readonly IDataGridService _dataGridService = dataGridService;

        [HttpGet]
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> Index(
            string searchTerm = "",
            int page = 1,
            string sortField = "Timestamp",
            string sortDirection = "desc")
        {
            DataGridViewModel viewModel = await _dataGridService.GetGridDataAsync(
                tableName: "Logs",
                page: page,
                searchTerm: searchTerm,
                sortField: sortField,
                sortDirection: sortDirection);
            return View(model: viewModel);
        }
    }
}
