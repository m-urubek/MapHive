using MapHive.Models.ViewModels;
using MapHive.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MapHive.Controllers
{
    [Authorize]
    public class DisplayController : Controller
    {
        private readonly IDisplayService _displayService;

        public DisplayController(IDisplayService displayService)
        {
            this._displayService = displayService;
        }

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
            DisplayItemViewModel vm = await this._displayService.GetItemAsync(tableName, id);
            // Pass metadata to view
            this.ViewBag.TableName = vm.TableName;
            this.ViewBag.ItemId = vm.ItemId;
            this.ViewBag.IsUsersTable = vm.IsUsersTable;
            if (vm.IsUsersTable)
            {
                this.ViewBag.Username = vm.Username;
            }
            // Render view with item data
            return this.View(vm.Data);
        }
    }
}