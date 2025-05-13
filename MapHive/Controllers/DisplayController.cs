namespace MapHive.Controllers
{
    using MapHive.Models.ViewModels;
    using MapHive.Services;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    [Authorize]
    public class DisplayController(IDisplayPageService displayService) : Controller
    {
        private readonly IDisplayPageService _displayService = displayService;

        /// <summary>
        /// Displays all data for a specific item from a table
        /// </summary>
        /// <param name="tableName">Name of the table</param>
        /// <param name="id">ID of the item</param>
        /// <returns>View with item data</returns>
        [HttpGet]
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> Item(string tableName, int id)
        {
            // Retrieve item and metadata via service
            DisplayItemViewModel vm = await _displayService.GetItemAsync(tableName: tableName, id: id);
            // Pass metadata to view
            ViewBag.TableName = vm.TableName;
            ViewBag.ItemId = vm.ItemId;
            ViewBag.IsUsersTable = vm.IsUsersTable;
            if (vm.IsUsersTable)
            {
                ViewBag.Username = vm.Username;
            }
            // Render view with item data
            return View(model: vm.Data);
        }
    }
}