namespace MapHive.Services
{
    using MapHive.Models;
    using MapHive.Models.Enums;
    using MapHive.Models.RepositoryModels;
    using MapHive.Models.ViewModels;

    public interface IAdminService
    {
        // Category management
        Task<IEnumerable<CategoryGet>> GetAllCategoriesAsync();
        Task AddCategoryAsync(CategoryCreate createDto);
        Task<CategoryGet?> GetCategoryByIdAsync(int id);
        Task UpdateCategoryAsync(CategoryUpdate updateDto);
        Task DeleteCategoryAsync(int id);
        Task UpdateUserTierAsync(int userId, UserTier tier);

        // SQL execution
        Task<SqlQueryViewModel> ExecuteSqlQueryAsync(string query);

        // Configuration management
        Task<List<ConfigurationItem>> GetAllConfigurationItemsAsync();
        Task AddConfigurationItemAsync(ConfigurationItem item);
        Task<ConfigurationItem?> GetConfigurationItemAsync(string key);
        Task UpdateConfigurationItemAsync(ConfigurationItem item);
        Task DeleteConfigurationItemAsync(string key);
        Task<BanDetailViewModel> GetBanDetailsAsync(int id);
        Task<bool> RemoveBanAsync(int id);

        /// <summary>
        /// Gets the data needed for the Ban User page in profile context.
        /// </summary>
        Task<BanUserPageViewModel> GetBanUserPageViewModelAsync(int adminId, string username);

        /// <summary>
        /// Processes a ban for a user or IP by an admin.
        /// </summary>
        Task<bool> BanUserAsync(int adminId, string username, BanViewModel model);
    }
}
