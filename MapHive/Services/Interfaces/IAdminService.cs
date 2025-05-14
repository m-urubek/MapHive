namespace MapHive.Services
{
    using MapHive.Models.Enums;
    using MapHive.Models.RepositoryModels;
    using MapHive.Models.ViewModels;

    public interface IAdminService
    {
        // CategoryName management
        Task<IEnumerable<CategoryGet>> GetAllCategoriesAsync();
        Task AddCategoryAsync(CategoryCreate createDto);
        Task<CategoryGet?> GetCategoryByIdAsync(int id);
        Task UpdateCategoryAsync(CategoryUpdate updateDto);
        Task DeleteCategoryAsync(int id);
        Task UpdateAccountTierAsync(int accountId, AccountTier tier);

        // SQL execution
        Task<SqlQueryViewModel> ExecuteSqlQueryAsync(string query);

        // Configuration management
        Task<List<ConfigurationItem>> GetAllConfigurationItemsAsync();
        Task AddConfigurationItemAsync(ConfigurationItem item);
        Task<ConfigurationItem?> GetConfigurationItemAsync(string key);
        Task UpdateConfigurationItemAsync(ConfigurationItem item);
        Task DeleteConfigurationItemAsync(string key);

        /// <summary>
        /// Gets the data needed for the Ban User page in profile context.
        /// </summary>
        Task<BanUserPageViewModel> GetBanUserPageViewModelAsync(int accountId);
        Task<int> BanAsync(BanViewModel banViewModel);
    }
}
