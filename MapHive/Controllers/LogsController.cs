using MapHive.Models;
using MapHive.Models.DataGrid;
using MapHive.Singletons;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MapHive.Controllers
{
    [Authorize]
    public class LogsController : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Index(string searchTerm = "", int page = 1, int pageSize = 20, 
            string sortField = "Timestamp", string sortDirection = "desc")
        {
            // Check if the user is an admin
            string? userTierClaim = this.User.FindFirst("UserTier")?.Value;
            if (string.IsNullOrEmpty(userTierClaim) || !int.TryParse(userTierClaim, out int userTierValue) 
                || (UserTier)userTierValue != UserTier.Admin)
            {
                return this.RedirectToAction("AccessDenied", "Account");
            }
            
            // Get logs using the DataGridRepository
            DataGrid viewModel = await CurrentRequest.DataGridRepository.GetGridDataAsync(
                "Logs",
                page,
                pageSize,
                searchTerm,
                sortField,
                sortDirection);

            return View(viewModel);
        }
    }
}