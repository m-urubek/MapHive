using MapHive.Models;
using MapHive.Singletons;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace MapHive.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            // Check if the user is an admin
            string? userTierClaim = this.User.FindFirst("UserTier")?.Value;
            return string.IsNullOrEmpty(userTierClaim) || !int.TryParse(userTierClaim, out int userTierValue) || (UserTier)userTierValue != UserTier.Admin
                ? this.Forbid()
                : this.View();
        }

        #region Categories Management

        [HttpGet]
        public async Task<IActionResult> Categories()
        {
            // Check if the user is an admin
            string? userTierClaim = this.User.FindFirst("UserTier")?.Value;
            if (string.IsNullOrEmpty(userTierClaim) || !int.TryParse(userTierClaim, out int userTierValue) || (UserTier)userTierValue != UserTier.Admin)
            {
                return this.Forbid();
            }

            IEnumerable<Category> categories = await CurrentRequest.MapRepository.GetAllCategoriesAsync();
            return this.View(categories);
        }

        [HttpGet]
        public IActionResult AddCategory()
        {
            // Check if the user is an admin
            string? userTierClaim = this.User.FindFirst("UserTier")?.Value;
            return string.IsNullOrEmpty(userTierClaim) || !int.TryParse(userTierClaim, out int userTierValue) || (UserTier)userTierValue != UserTier.Admin
                ? this.Forbid()
                : this.View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory(Category category)
        {
            // Check if the user is an admin
            string? userTierClaim = this.User.FindFirst("UserTier")?.Value;
            if (string.IsNullOrEmpty(userTierClaim) || !int.TryParse(userTierClaim, out int userTierValue) || (UserTier)userTierValue != UserTier.Admin)
            {
                return this.Forbid();
            }

            if (!this.ModelState.IsValid)
            {
                return this.View(category);
            }

            _ = await CurrentRequest.MapRepository.AddCategoryAsync(category);
            return this.RedirectToAction("Categories");
        }

        [HttpGet]
        public async Task<IActionResult> EditCategory(int id)
        {
            // Check if the user is an admin
            string? userTierClaim = this.User.FindFirst("UserTier")?.Value;
            if (string.IsNullOrEmpty(userTierClaim) || !int.TryParse(userTierClaim, out int userTierValue) || (UserTier)userTierValue != UserTier.Admin)
            {
                return this.Forbid();
            }

            Category? category = await CurrentRequest.MapRepository.GetCategoryByIdAsync(id);
            return category == null ? this.NotFound() : this.View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(Category category)
        {
            // Check if the user is an admin
            string? userTierClaim = this.User.FindFirst("UserTier")?.Value;
            if (string.IsNullOrEmpty(userTierClaim) || !int.TryParse(userTierClaim, out int userTierValue) || (UserTier)userTierValue != UserTier.Admin)
            {
                return this.Forbid();
            }

            if (!this.ModelState.IsValid)
            {
                return this.View(category);
            }

            _ = await CurrentRequest.MapRepository.UpdateCategoryAsync(category);
            return this.RedirectToAction("Categories");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            // Check if the user is an admin
            string? userTierClaim = this.User.FindFirst("UserTier")?.Value;
            if (string.IsNullOrEmpty(userTierClaim) || !int.TryParse(userTierClaim, out int userTierValue) || (UserTier)userTierValue != UserTier.Admin)
            {
                return this.Forbid();
            }

            _ = await CurrentRequest.MapRepository.DeleteCategoryAsync(id);
            return this.RedirectToAction("Categories");
        }

        #endregion

        #region Users Management

        [HttpGet]
        public async Task<IActionResult> Users(string searchTerm = "", int page = 1, int pageSize = 20, string sortField = "", string sortDirection = "asc")
        {
            // Check if the user is an admin
            string? userTierClaim = this.User.FindFirst("UserTier")?.Value;
            if (string.IsNullOrEmpty(userTierClaim) || !int.TryParse(userTierClaim, out int userTierValue) || (UserTier)userTierValue != UserTier.Admin)
            {
                return this.Forbid();
            }

            IEnumerable<User> users = await CurrentRequest.UserRepository.GetUsersAsync(searchTerm, page, pageSize, sortField, sortDirection);
            int totalUsers = await CurrentRequest.UserRepository.GetTotalUsersCountAsync(searchTerm);

            UsersGridViewModel viewModel = new()
            {
                Users = users,
                SearchTerm = searchTerm,
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalUsers
            };
            
            // Initialize the grid with columns and data
            viewModel.InitializeGrid();

            // Store sort information in ViewData to be used in the view
            this.ViewData["SortField"] = sortField;
            this.ViewData["SortDirection"] = sortDirection;

            return this.View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserTier(int userId, UserTier tier)
        {
            // Check if the user is an admin
            string? userTierClaim = this.User.FindFirst("UserTier")?.Value;
            if (string.IsNullOrEmpty(userTierClaim) || !int.TryParse(userTierClaim, out int userTierValue) || (UserTier)userTierValue != UserTier.Admin)
            {
                return this.Forbid();
            }

            _ = await CurrentRequest.UserRepository.UpdateUserTierAsync(userId, tier);
            return this.RedirectToAction("Users");
        }

        #endregion

        #region SQL Execution

        [HttpGet]
        public IActionResult SqlQuery()
        {
            // Check if the user is an admin
            string? userTierClaim = this.User.FindFirst("UserTier")?.Value;
            return string.IsNullOrEmpty(userTierClaim) || !int.TryParse(userTierClaim, out int userTierValue) || (UserTier)userTierValue != UserTier.Admin
                ? this.Forbid()
                : this.View(new SqlQueryViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SqlQuery(SqlQueryViewModel model)
        {
            // Check if the user is an admin
            string? userTierClaim = this.User.FindFirst("UserTier")?.Value;
            if (string.IsNullOrEmpty(userTierClaim) || !int.TryParse(userTierClaim, out int userTierValue) || (UserTier)userTierValue != UserTier.Admin)
            {
                return this.Forbid();
            }

            if (!this.ModelState.IsValid)
            {
                return this.View(model);
            }

            try
            {
                string query = model.Query.Trim();

                // Execute based on query type
                if (query.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                {
                    DataTable result = await CurrentRequest.SqlClient.SelectAsync(query);
                    model.HasResults = true;
                    model.DataTable = result;
                    model.RowsAffected = result.Rows.Count;
                    model.Message = $"Query executed successfully. {model.RowsAffected} rows returned.";
                }
                else if (query.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase))
                {
                    int result = await CurrentRequest.SqlClient.InsertAsync(query);
                    model.HasResults = false;
                    model.RowsAffected = 1;
                    model.Message = $"Insert executed successfully. ID of inserted row: {result}";
                }
                else if (query.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase))
                {
                    int result = await CurrentRequest.SqlClient.UpdateAsync(query);
                    model.HasResults = false;
                    model.RowsAffected = result;
                    model.Message = $"Update executed successfully. {result} rows affected.";
                }
                else if (query.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase))
                {
                    int result = await CurrentRequest.SqlClient.DeleteAsync(query);
                    model.HasResults = false;
                    model.RowsAffected = result;
                    model.Message = $"Delete executed successfully. {result} rows affected.";
                }
                else
                {
                    // For other statements like ALTER, CREATE, etc.
                    int result = await CurrentRequest.SqlClient.AlterAsync(query);
                    model.HasResults = false;
                    model.RowsAffected = result;
                    model.Message = "Query executed successfully.";
                }
            }
            catch (Exception ex)
            {
                model.HasResults = false;
                model.Message = $"Error executing query: {ex.Message}";
            }

            return this.View(model);
        }

        #endregion

        #region Configuration Management

        [HttpGet]
        public async Task<IActionResult> Configuration()
        {
            // Check if the user is an admin
            string? userTierClaim = this.User.FindFirst("UserTier")?.Value;
            if (string.IsNullOrEmpty(userTierClaim) || !int.TryParse(userTierClaim, out int userTierValue) || (UserTier)userTierValue != UserTier.Admin)
            {
                return this.Forbid();
            }

            List<ConfigurationItem> configurations = await CurrentRequest.ConfigService.GetAllConfigurationItemsAsync();
            return this.View(configurations);
        }

        [HttpGet]
        public IActionResult AddConfiguration()
        {
            // Check if the user is an admin
            string? userTierClaim = this.User.FindFirst("UserTier")?.Value;
            return string.IsNullOrEmpty(userTierClaim) || !int.TryParse(userTierClaim, out int userTierValue) || (UserTier)userTierValue != UserTier.Admin
                ? this.Forbid()
                : this.View(new ConfigurationItem());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddConfiguration(ConfigurationItem configItem)
        {
            // Check if the user is an admin
            string? userTierClaim = this.User.FindFirst("UserTier")?.Value;
            if (string.IsNullOrEmpty(userTierClaim) || !int.TryParse(userTierClaim, out int userTierValue) || (UserTier)userTierValue != UserTier.Admin)
            {
                return this.Forbid();
            }

            if (!this.ModelState.IsValid)
            {
                return this.View(configItem);
            }

            _ = await CurrentRequest.ConfigService.AddConfigurationItemAsync(configItem);
            return this.RedirectToAction("Configuration");
        }

        [HttpGet]
        public async Task<IActionResult> EditConfiguration(string key)
        {
            // Check if the user is an admin
            string? userTierClaim = this.User.FindFirst("UserTier")?.Value;
            if (string.IsNullOrEmpty(userTierClaim) || !int.TryParse(userTierClaim, out int userTierValue) || (UserTier)userTierValue != UserTier.Admin)
            {
                return this.Forbid();
            }

            ConfigurationItem? configItem = await CurrentRequest.ConfigService.GetConfigurationItemAsync(key);
            return configItem == null ? this.NotFound() : this.View(configItem);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditConfiguration(ConfigurationItem configItem)
        {
            // Check if the user is an admin
            string? userTierClaim = this.User.FindFirst("UserTier")?.Value;
            if (string.IsNullOrEmpty(userTierClaim) || !int.TryParse(userTierClaim, out int userTierValue) || (UserTier)userTierValue != UserTier.Admin)
            {
                return this.Forbid();
            }

            if (!this.ModelState.IsValid)
            {
                return this.View(configItem);
            }

            _ = await CurrentRequest.ConfigService.UpdateConfigurationItemAsync(configItem);
            return this.RedirectToAction("Configuration");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfiguration(string key)
        {
            // Check if the user is an admin
            string? userTierClaim = this.User.FindFirst("UserTier")?.Value;
            if (string.IsNullOrEmpty(userTierClaim) || !int.TryParse(userTierClaim, out int userTierValue) || (UserTier)userTierValue != UserTier.Admin)
            {
                return this.Forbid();
            }

            _ = await CurrentRequest.ConfigService.DeleteConfigurationItemAsync(key);
            return this.RedirectToAction("Configuration");
        }

        #endregion

        #region Ban Management

        [HttpGet]
        public async Task<IActionResult> Bans(string searchTerm = "", int page = 1, int pageSize = 20, string sortField = "", string sortDirection = "asc")
        {
            // Check if the user is an admin
            string? userTierClaim = this.User.FindFirst("UserTier")?.Value;
            if (string.IsNullOrEmpty(userTierClaim) || !int.TryParse(userTierClaim, out int userTierValue) || (UserTier)userTierValue != UserTier.Admin)
            {
                return this.Forbid();
            }

            IEnumerable<UserBan> bans = await CurrentRequest.UserRepository.GetAllBansAsync(searchTerm, page, pageSize, sortField, sortDirection);
            int totalBans = await CurrentRequest.UserRepository.GetTotalBansCountAsync(searchTerm);

            BansGridViewModel viewModel = new()
            {
                Bans = bans,
                SearchTerm = searchTerm,
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalBans
            };
            
            // Initialize the grid with columns and data
            viewModel.InitializeGrid();

            // Store sort information in ViewData to be used in the view
            this.ViewData["SortField"] = sortField;
            this.ViewData["SortDirection"] = sortDirection;

            return this.View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> BanDetails(int id)
        {
            // Check if the user is an admin
            string? userTierClaim = this.User.FindFirst("UserTier")?.Value;
            if (string.IsNullOrEmpty(userTierClaim) || !int.TryParse(userTierClaim, out int userTierValue) || (UserTier)userTierValue != UserTier.Admin)
            {
                return this.Forbid();
            }

            UserBan? ban = await CurrentRequest.UserRepository.GetBanByIdAsync(id);
            if (ban == null)
            {
                return this.NotFound();
            }

            string bannedUsername = string.Empty;
            string bannedByUsername = string.Empty;

            if (ban.Properties.TryGetValue("BannedUsername", out string? username))
            {
                bannedUsername = username;
            }
            else if (ban.UserId.HasValue)
            {
                // Get username from UserId
                bannedUsername = await CurrentRequest.UserRepository.GetUsernameByIdAsync(ban.UserId.Value);
            }

            if (ban.Properties.TryGetValue("BannedByUsername", out string? adminUsername))
            {
                bannedByUsername = adminUsername;
            }
            else
            {
                // Get admin username
                bannedByUsername = await CurrentRequest.UserRepository.GetUsernameByIdAsync(ban.BannedByUserId);
            }

            BanDetailViewModel viewModel = new()
            {
                Ban = ban,
                BannedUsername = bannedUsername,
                BannedByUsername = bannedByUsername
            };

            return this.View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveBan(int id)
        {
            // Check if the user is an admin
            string? userTierClaim = this.User.FindFirst("UserTier")?.Value;
            if (string.IsNullOrEmpty(userTierClaim) || !int.TryParse(userTierClaim, out int userTierValue) || (UserTier)userTierValue != UserTier.Admin)
            {
                return this.Forbid();
            }

            UserBan? ban = await CurrentRequest.UserRepository.GetBanByIdAsync(id);
            if (ban == null)
            {
                return this.NotFound();
            }

            bool success = await CurrentRequest.UserRepository.UnbanUserAsync(id);

            if (success)
            {
                this.TempData["SuccessMessage"] = "Ban has been removed successfully.";
            }
            else
            {
                this.TempData["ErrorMessage"] = "Failed to remove ban. Please try again.";
            }

            return this.RedirectToAction("Bans");
        }

        // AJAX endpoint for grid data
        [HttpGet]
        public async Task<IActionResult> GetGridData(string gridId, int page = 1, string searchTerm = "", string sortField = "", string sortDirection = "asc")
        {
            // Check if the user is an admin
            string? userTierClaim = this.User.FindFirst("UserTier")?.Value;
            if (string.IsNullOrEmpty(userTierClaim) || !int.TryParse(userTierClaim, out int userTierValue) || (UserTier)userTierValue != UserTier.Admin)
            {
                return this.Forbid();
            }

            // Default page size
            int pageSize = 20;
            
            if (gridId == "userGrid")
            {
                // Load user data
                IEnumerable<User> users = await CurrentRequest.UserRepository.GetUsersAsync(searchTerm, page, pageSize, sortField, sortDirection);
                int totalUsers = await CurrentRequest.UserRepository.GetTotalUsersCountAsync(searchTerm);

                UsersGridViewModel viewModel = new()
                {
                    Users = users,
                    SearchTerm = searchTerm,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalUsers
                };
                
                // Initialize grid with columns and data
                viewModel.InitializeGrid();
                
                return this.Json(new { 
                    success = true,
                    totalPages = viewModel.TotalPages,
                    currentPage = page,
                    totalCount = totalUsers,
                    items = viewModel.Grid.Items,
                    gridId = gridId
                });
            }
            else if (gridId == "banGrid")
            {
                // Load ban data
                IEnumerable<UserBan> bans = await CurrentRequest.UserRepository.GetAllBansAsync(searchTerm, page, pageSize, sortField, sortDirection);
                int totalBans = await CurrentRequest.UserRepository.GetTotalBansCountAsync(searchTerm);

                BansGridViewModel viewModel = new()
                {
                    Bans = bans,
                    SearchTerm = searchTerm,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalBans
                };
                
                // Initialize grid with columns and data
                viewModel.InitializeGrid();
                
                return this.Json(new { 
                    success = true,
                    totalPages = viewModel.TotalPages,
                    currentPage = page,
                    totalCount = totalBans,
                    items = viewModel.Grid.Items,
                    gridId = gridId
                });
            }

            return this.Json(new { success = false, message = "Invalid grid ID" });
        }

        #endregion
    }
}