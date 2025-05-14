namespace MapHive.Repositories
{
    using MapHive.Models.RepositoryModels;

    public interface IAccountsRepository
    {
        /// <summary>
        /// Create a new user
        /// </summary>
        Task<int> CreateAccountAsync(UserCreate createDto);

        // Basic user lookup
        Task<string> GetUsernameByIdAsync(int accountId);
        Task<AccountGet?> GetAccountByUsernameAsync(string username);
        Task<AccountGet> GetAccountByUsernameOrThrowAsync(string username);
        Task<bool> CheckUsernameExistsAsync(string username);
        Task<AccountGet?> GetAccountByIdAsync(int id);
        Task<AccountGet> GetAccountByIdOrThrowAsync(int id);

        // Admin methods
        Task<IEnumerable<AccountGet>> GeAccountsAsync(string searchTerm, int page, int pageSize, string sortColumnName = "", string sortDirection = "asc");
        Task<int> GetTotalAccountsCountAsync(string searchTerm);
        Task<bool> UpdateAccountTierAsync(AccountTierUpdate tierDto);
        Task<int> UpdateAccountAsync(UserUpdate updateDto);
    }
}
