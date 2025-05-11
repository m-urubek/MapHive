namespace MapHive.Controllers
{
    using AutoMapper;
    using MapHive.Models.Enums;
    using MapHive.Models.RepositoryModels;
    using MapHive.Models.ViewModels;
    using MapHive.Services;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    [Authorize]
    public class AdminController(IAdminService adminService, IMapper mapper, IDataGridService dataGridService) : Controller
    {
        private readonly IAdminService _adminService = adminService;
        private readonly IMapper _mapper = mapper;
        private readonly IDataGridService _dataGridService = dataGridService;

        [HttpGet]
        [Authorize(Roles = "Admin,2")]
        public IActionResult Index()
        {
            return View();
        }

        #region Categories Management

        [HttpGet]
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> Categories()
        {
            IEnumerable<CategoryGet> categories = await _adminService.GetAllCategoriesAsync();
            return View(model: categories);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> AddCategory(CategoryCreate categoryCreate)
        {
            if (!ModelState.IsValid)
            {
                return View(model: categoryCreate);
            }

            await _adminService.AddCategoryAsync(createDto: categoryCreate);
            return RedirectToAction(actionName: "Categories");
        }

        [HttpGet]
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> EditCategory(int id)
        {
            CategoryGet? categoryGet = await _adminService.GetCategoryByIdAsync(id: id);
            if (categoryGet == null)
            {
                return NotFound();
            }
            CategoryUpdate categoryDto = _mapper.Map<CategoryUpdate>(source: categoryGet);
            return View(model: categoryDto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> EditCategory(CategoryUpdate categoryUpdate)
        {
            if (!ModelState.IsValid)
            {
                return View(model: categoryUpdate);
            }

            await _adminService.UpdateCategoryAsync(updateDto: categoryUpdate);
            return RedirectToAction(actionName: "Categories");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            await _adminService.DeleteCategoryAsync(id: id);
            return RedirectToAction(actionName: "Categories");
        }

        #endregion

        #region Users Management

        [HttpGet]
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> Users()
        {
            DataGridViewModel viewModel = new()
            {
                Title = "Manage Users",
                TableName = "Users",
                ColumnNames = new List<string>(),
                Columns = await _dataGridService.GetColumnsForTableAsync(tableName: "Users")
            };
            return View("_DataGrid", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> UpdateUserTier(int userId, UserTier userTier)
        {
            await _adminService.UpdateUserTierAsync(userId: userId, tier: userTier);
            return RedirectToAction(actionName: "Users");
        }

        #endregion

        #region SQL Execution

        [HttpGet]
        [Authorize(Roles = "Admin,2")]
        public IActionResult SqlQuery()
        {
            return View(model: new SqlQueryViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> SqlQuery(SqlQueryViewModel sqlQueryViewModel)
        {
            if (!ModelState.IsValid)
            {
                return View(model: sqlQueryViewModel);
            }

            SqlQueryViewModel resultModel = await _adminService.ExecuteSqlQueryAsync(query: sqlQueryViewModel.Query);
            return View(model: resultModel);
        }

        #endregion

        #region Configuration Management

        [HttpGet]
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> Configuration()
        {
            List<ConfigurationItem> configurations = await _adminService.GetAllConfigurationItemsAsync();
            return View(model: configurations);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,2")]
        public IActionResult AddConfiguration()
        {
            return View(model: new ConfigurationItem());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> AddConfiguration(ConfigurationItem configurationItem)
        {
            if (!ModelState.IsValid)
            {
                return View(model: configurationItem);
            }

            await _adminService.AddConfigurationItemAsync(item: configurationItem);
            return RedirectToAction(actionName: "Configuration");
        }

        [HttpGet]
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> EditConfiguration(string key)
        {
            ConfigurationItem? configItem = await _adminService.GetConfigurationItemAsync(key: key);
            return configItem == null ? NotFound() : View(model: configItem);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> EditConfiguration(ConfigurationItem configurationItem)
        {
            if (!ModelState.IsValid)
            {
                return View(model: configurationItem);
            }

            await _adminService.UpdateConfigurationItemAsync(item: configurationItem);
            return RedirectToAction(actionName: "Configuration");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> DeleteConfiguration(string key)
        {
            await _adminService.DeleteConfigurationItemAsync(key: key);
            return RedirectToAction(actionName: "Configuration");
        }

        #endregion

        #region Logs Management

        [HttpGet]
        [Authorize(Roles = "Admin,2")]
        public async Task<IActionResult> Logs()
        {
            DataGridViewModel viewModel = new()
            {
                Title = "System Logs",
                TableName = "Logs",
                ColumnNames = new List<string> { "Id_Log", "Timestamp", "SeverityId", "Message", "UserId", "RequestPath", "Source", "AdditionalData" },
                Columns = await _dataGridService.GetColumnsForTableAsync(tableName: "Logs")
            };
            return View("_DataGrid", viewModel);
        }

        #endregion
    }
}
