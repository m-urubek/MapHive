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
    public class IpBansController(
        IDataGridService dataGridService,
        IIpBansRepository ipBansRepository) : Controller
    {
        private readonly IDataGridService _dataGridService = dataGridService;
        private readonly IIpBansRepository _ipBansRepository = ipBansRepository;

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
            bool success = await _ipBansRepository.RemoveIpBanAsync(banId: id);
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
