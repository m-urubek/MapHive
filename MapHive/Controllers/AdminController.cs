using AutoMapper;
using MapHive.Models;
using MapHive.Models.Enums;
using MapHive.Models.RepositoryModels;
using MapHive.Models.ViewModels;
using MapHive.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MapHive.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly IMapper _mapper;

        public AdminController(IAdminService adminService, IMapper mapper)
        {
            this._adminService = adminService;
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
            IEnumerable<CategoryGet> categories = await this._adminService.GetAllCategoriesAsync();
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
        public async Task<IActionResult> AddCategory(CategoryCreate categoryCreate)
        {
            if (!this.ModelState.IsValid)
            {
                return this.View(categoryCreate);
            }

            await this._adminService.AddCategoryAsync(categoryCreate);
            return this.RedirectToAction("Categories");
        }

        [HttpGet]
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> EditCategory(int id)
        {
            CategoryGet? categoryGet = await this._adminService.GetCategoryByIdAsync(id);
            if (categoryGet == null)
            {
                return this.NotFound();
            }
            CategoryUpdate categoryDto = this._mapper.Map<CategoryUpdate>(categoryGet);
            return this.View(categoryDto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> EditCategory(CategoryUpdate categoryUpdate)
        {
            if (!this.ModelState.IsValid)
            {
                return this.View(categoryUpdate);
            }

            await this._adminService.UpdateCategoryAsync(categoryUpdate);
            return this.RedirectToAction("Categories");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            await this._adminService.DeleteCategoryAsync(id);
            return this.RedirectToAction("Categories");
        }

        #endregion

        #region Users Management

        [HttpGet]
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> Users(string searchTerm = "", int page = 1, int pageSize = 20, string sortField = "", string sortDirection = "asc")
        {
            DataGridViewModel viewModel = await this._adminService.GetUsersGridViewModelAsync(searchTerm, page, pageSize, sortField, sortDirection);
            return this.View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> UpdateUserTier(int userId, UserTier userTier)
        {
            await this._adminService.UpdateUserTierAsync(userId, userTier);
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
        public async Task<IActionResult> SqlQuery(SqlQueryViewModel sqlQueryViewModel)
        {
            if (!this.ModelState.IsValid)
            {
                return this.View(sqlQueryViewModel);
            }

            SqlQueryViewModel resultModel = await this._adminService.ExecuteSqlQueryAsync(sqlQueryViewModel.Query);
            return this.View(resultModel);
        }

        #endregion

        #region Configuration Management

        [HttpGet]
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> Configuration()
        {
            List<ConfigurationItem> configurations = await this._adminService.GetAllConfigurationItemsAsync();
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
        public async Task<IActionResult> AddConfiguration(ConfigurationItem configurationItem)
        {
            if (!this.ModelState.IsValid)
            {
                return this.View(configurationItem);
            }

            await this._adminService.AddConfigurationItemAsync(configurationItem);
            return this.RedirectToAction("Configuration");
        }

        [HttpGet]
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> EditConfiguration(string key)
        {
            ConfigurationItem? configItem = await this._adminService.GetConfigurationItemAsync(key);
            return configItem == null ? this.NotFound() : this.View(configItem);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> EditConfiguration(ConfigurationItem configurationItem)
        {
            if (!this.ModelState.IsValid)
            {
                return this.View(configurationItem);
            }

            await this._adminService.UpdateConfigurationItemAsync(configurationItem);
            return this.RedirectToAction("Configuration");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> DeleteConfiguration(string key)
        {
            await this._adminService.DeleteConfigurationItemAsync(key);
            return this.RedirectToAction("Configuration");
        }

        #endregion

        #region Ban Management

        [HttpGet]
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> Bans(string searchTerm = "", int page = 1, int pageSize = 20, string sortField = "", string sortDirection = "asc")
        {
            DataGridViewModel viewModel = await this._adminService.GetBansGridViewModelAsync(searchTerm, page, pageSize, sortField, sortDirection);
            this.ViewData["SortField"] = sortField;
            this.ViewData["SortDirection"] = sortDirection;
            return this.View(viewModel);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> BanDetails(int id)
        {
            try
            {
                BanDetailViewModel viewModel = await this._adminService.GetBanDetailsAsync(id);
                return this.View(viewModel);
            }
            catch (KeyNotFoundException)
            {
                return this.NotFound();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> RemoveBan(int id)
        {
            bool success = await this._adminService.RemoveBanAsync(id);
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