using AutoMapper;
using MapHive.Models;
using MapHive.Models.Enums;
using MapHive.Models.RepositoryModels;
using MapHive.Models.ViewModels;
using MapHive.Repositories;
using MapHive.Singletons;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace MapHive.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly ISqlClientSingleton _sqlClient;
        private readonly IDataGridRepository _dataGridRepository;
        private readonly IConfigurationSingleton _configSingleton;
        private readonly IMapLocationRepository _mapLocationRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        public AdminController(
            ISqlClientSingleton sqlClient,
            IDataGridRepository dataGridRepository,
            IConfigurationSingleton configSingleton,
            IMapLocationRepository mapLocationRepository,
            IUserRepository userRepository,
            IMapper mapper)
        {
            this._sqlClient = sqlClient;
            this._dataGridRepository = dataGridRepository;
            this._configSingleton = configSingleton;
            this._mapLocationRepository = mapLocationRepository;
            this._userRepository = userRepository;
            this._mapper = mapper;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,2")]
        public IActionResult Index()
        {
            return this.View();
        }

        #region Categories Management

        [HttpGet]
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> Categories()
        {
            IEnumerable<CategoryGet> categories = await this._mapLocationRepository.GetAllCategoriesAsync();
            return this.View(categories);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,2")]
        public IActionResult AddCategory()
        {
            return this.View(new CategoryCreate());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> AddCategory(CategoryCreate categoryDto)
        {
            if (!this.ModelState.IsValid)
            {
                return this.View(categoryDto);
            }

            _ = await this._mapLocationRepository.AddCategoryAsync(categoryDto);
            return this.RedirectToAction("Categories");
        }

        [HttpGet]
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> EditCategory(int id)
        {
            CategoryGet? categoryGet = await this._mapLocationRepository.GetCategoryByIdAsync(id);
            if (categoryGet == null)
            {
                return this.NotFound();
            }
            CategoryUpdate categoryDto = new()
            {
                Id = categoryGet.Id,
                Name = categoryGet.Name,
                Description = categoryGet.Description,
                Icon = categoryGet.Icon
            };
            return this.View(categoryDto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> EditCategory(CategoryUpdate categoryDto)
        {
            if (!this.ModelState.IsValid)
            {
                return this.View(categoryDto);
            }

            _ = await this._mapLocationRepository.UpdateCategoryAsync(categoryDto);
            return this.RedirectToAction("Categories");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            _ = await this._mapLocationRepository.DeleteCategoryAsync(id);
            return this.RedirectToAction("Categories");
        }

        #endregion

        #region Users Management

        [HttpGet]
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> Users(string searchTerm = "", int page = 1, int pageSize = 20, string sortField = "", string sortDirection = "asc")
        {
            // Get users using the DataGridRepository
            DataGridGet dataGridGet = await this._dataGridRepository.GetGridDataAsync(
                "Users",
                page,
                pageSize,
                searchTerm,
                sortField,
                sortDirection);

            return this.View(this._mapper.Map<DataGridViewModel>(dataGridGet));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> UpdateUserTier(int userId, UserTier tier)
        {
            // Use UserTierUpdate DTO for updating user tier
            UserTierUpdate tierDto = new() { UserId = userId, Tier = tier };
            _ = await this._userRepository.UpdateUserTierAsync(tierDto);
            return this.RedirectToAction("Users");
        }

        #endregion

        #region SQL Execution

        [HttpGet]
        [Authorize(Roles = "Admin,2")]
        public IActionResult SqlQuery()
        {
            return this.View(new SqlQueryViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> SqlQuery(SqlQueryViewModel model)
        {
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
                    DataTable result = await this._sqlClient.SelectAsync(query);
                    model.HasResults = true;
                    model.DataTable = result;
                    model.RowsAffected = result.Rows.Count;
                    model.Message = $"Query executed successfully. {model.RowsAffected} rows returned.";
                }
                else if (query.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase))
                {
                    int result = await this._sqlClient.InsertAsync(query);
                    model.HasResults = false;
                    model.RowsAffected = 1;
                    model.Message = $"Insert executed successfully. ID of inserted row: {result}";
                }
                else if (query.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase))
                {
                    int result = await this._sqlClient.UpdateAsync(query);
                    model.HasResults = false;
                    model.RowsAffected = result;
                    model.Message = $"Update executed successfully. {result} rows affected.";
                }
                else if (query.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase))
                {
                    int result = await this._sqlClient.DeleteAsync(query);
                    model.HasResults = false;
                    model.RowsAffected = result;
                    model.Message = $"Delete executed successfully. {result} rows affected.";
                }
                else
                {
                    // For other statements like ALTER, CREATE, etc.
                    int result = await this._sqlClient.AlterAsync(query);
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
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> Configuration()
        {
            List<ConfigurationItem> configurations = await this._configSingleton.GetAllConfigurationItemsAsync();
            return this.View(configurations);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,2")]
        public IActionResult AddConfiguration()
        {
            return this.View(new ConfigurationItem());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> AddConfiguration(ConfigurationItem configItem)
        {
            if (!this.ModelState.IsValid)
            {
                return this.View(configItem);
            }

            _ = await this._configSingleton.AddConfigurationItemAsync(configItem);
            return this.RedirectToAction("Configuration");
        }

        [HttpGet]
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> EditConfiguration(string key)
        {
            ConfigurationItem? configItem = await this._configSingleton.GetConfigurationItemAsync(key);
            return configItem == null ? this.NotFound() : this.View(configItem);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> EditConfiguration(ConfigurationItem configItem)
        {
            if (!this.ModelState.IsValid)
            {
                return this.View(configItem);
            }

            _ = await this._configSingleton.UpdateConfigurationItemAsync(configItem);
            return this.RedirectToAction("Configuration");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> DeleteConfiguration(string key)
        {
            _ = await this._configSingleton.DeleteConfigurationItemAsync(key);
            return this.RedirectToAction("Configuration");
        }

        #endregion

        #region Ban Management

        [HttpGet]
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> Bans(string searchTerm = "", int page = 1, int pageSize = 20, string sortField = "", string sortDirection = "asc")
        {
            // Get bans using the DataGridRepository
            DataGridGet dataGridGet = await this._dataGridRepository.GetGridDataAsync(
                "UserBans",
                page,
                pageSize,
                searchTerm,
                sortField,
                sortDirection);

            DataGridViewModel dataGridViewModel = this._mapper.Map<DataGridViewModel>(dataGridGet);

            // Store sort information in ViewData to be used in the view
            this.ViewData["SortField"] = sortField;
            this.ViewData["SortDirection"] = sortDirection;

            return this.View(dataGridViewModel);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> BanDetails(int id)
        {
            UserBanGet? ban = await this._userRepository.GetActiveBanByUserIdAsync(id);
            if (ban == null)
            {
                return this.NotFound();
            }

            string bannedUsername = string.Empty;
            if (ban.Properties.TryGetValue("BannedUsername", out string? username))
            {
                bannedUsername = username;
            }
            else if (ban.UserId.HasValue)
            {
                // Get username from UserId
                bannedUsername = await this._userRepository.GetUsernameByIdAsync(ban.UserId.Value);
            }

            string bannedByUsername;
            if (ban.Properties.TryGetValue("BannedByUsername", out string? adminUsername))
            {
                bannedByUsername = adminUsername;
            }
            else
            {
                // Get admin username
                bannedByUsername = await this._userRepository.GetUsernameByIdAsync(ban.BannedByUserId);
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
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> RemoveBan(int id)
        {
            UserBanGet? ban = await this._userRepository.GetActiveBanByUserIdAsync(id);
            if (ban == null)
            {
                return this.NotFound();
            }

            bool success = await this._userRepository.UnbanUserAsync(id);

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

        #endregion
    }
}