namespace MapHive.Controllers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using MapHive.Models.ViewModels;
    using MapHive.Services;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using MapHive.Repositories;

    [Authorize]
    public class AccountBansController(
        IDataGridService dataGridService,
        IAccountBansRepository accountBansRepository) : Controller
    {
        private readonly IDataGridService _dataGridService = dataGridService;
        private readonly IAccountBansRepository _accountBansRepository = accountBansRepository;

        [HttpGet]
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> Index()
        {
            DataGridViewModel viewModel = new()
            {
                Title = "Manage User Bans",
                TableName = "AccountBans",
                ColumnNames = new List<string>(),
                Columns = await _dataGridService.GetColumnsForTableAsync(tableName: "AccountBans")
            };
            return View("_DataGrid", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> Remove(int id)
        {
            bool success = await _accountBansRepository.RemoveAccountBanAsync(banId: id);
            if (success)
            {
                TempData["SuccessMessage"] = "User ban removed successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to remove user ban.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
