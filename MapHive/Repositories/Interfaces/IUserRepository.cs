namespace MapHive.Repositories
{
    using MapHive.Models.RepositoryModels;

    public interface IUserRepository
    {
        /// <summary>
        /// Create a new user
        /// </summary>
        Task<int> CreateUserAsync(UserCreate createDto);

        // Basic user lookup
        Task<string> GetUsernameByIdAsync(int userId);
        Task<UserGet?> GetUserByUsernameAsync(string username);
        Task<bool> CheckUsernameExistsAsync(string username);
        Task<UserGet?> GetUserByIdAsync(int id);

        // Ban related methods
        Task<int> BanUserAsync(UserBanGetCreate banDto);
        Task<bool> UnbanUserAsync(int banId);
        Task<UserBanGet?> GetActiveBanByUserIdAsync(int userId);
        Task<UserBanGet?> GetActiveBanByIpAddressAsync(string hashedIpAddress);
        Task<IEnumerable<UserBanGet>> GetBanHistoryByUserIdAsync(int userId);
        Task<IEnumerable<UserBanGet>> GetAllActiveBansAsync();
        Task<IEnumerable<UserBanGet>> GetAllBansAsync(string searchTerm = "", int page = 1, int pageSize = 20, string sortColumnName = "", string sortDirection = "asc");
        Task<bool> IsIpBannedAsync(string hashedIpAddress);

        // Admin methods
        Task<IEnumerable<UserGet>> GetUsersAsync(string searchTerm, int page, int pageSize, string sortColumnName = "", string sortDirection = "asc");
        Task<int> GetTotalUsersCountAsync(string searchTerm);
        Task<bool> UpdateUserTierAsync(UserTierUpdate tierDto);
        Task<int> UpdateUserAsync(UserUpdate updateDto);
    }
}