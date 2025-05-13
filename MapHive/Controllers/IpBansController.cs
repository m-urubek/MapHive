namespace MapHive.Controllers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using MapHive.Models.ViewModels;
    using MapHive.Services;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    [Authorize]
    public class IpBansController(
        IDataGridService dataGridService,
        IIpBansService ipBansService) : Controller
    {
        private readonly IDataGridService _dataGridService = dataGridService;
        private readonly IIpBansService _ipBansService = ipBansService;

        [HttpGet]
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> Index()
        {
            DataGridViewModel viewModel = new()
            {
                Title = "Manage IP Bans",
                TableName = "IpBans",
                ColumnNames = new List<string>(),
                Columns = await _dataGridService.GetColumnsForTableAsync(tableName: "IpBans")
            };
            return View("_DataGrid", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> Remove(int id)
        {
            bool success = await _ipBansService.RemoveIpBanAsync(banId: id);
            if (success)
            {
                TempData["SuccessMessage"] = "IP ban removed successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to remove IP ban.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
