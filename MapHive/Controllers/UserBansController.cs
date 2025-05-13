namespace MapHive.Controllers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using MapHive.Models.ViewModels;
    using MapHive.Services;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    [Authorize]
    public class UserBansController(
        IDataGridService dataGridService,
        IUserBansService userBansService) : Controller
    {
        private readonly IDataGridService _dataGridService = dataGridService;
        private readonly IUserBansService _userBansService = userBansService;

        [HttpGet]
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> Index()
        {
            DataGridViewModel viewModel = new()
            {
                Title = "Manage User Bans",
                TableName = "UserBans",
                ColumnNames = new List<string>(),
                Columns = await _dataGridService.GetColumnsForTableAsync(tableName: "UserBans")
            };
            return View("_DataGrid", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> Remove(int id)
        {
            bool success = await _userBansService.RemoveUserBanAsync(banId: id);
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
