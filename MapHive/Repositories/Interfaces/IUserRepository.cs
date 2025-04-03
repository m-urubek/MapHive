using MapHive.Models;

namespace MapHive.Repositories
{
    public interface IUserRepository
    {
        int CreateUser(User user);
        User? GetUserById(int id);
        User? GetUserByUsername(string username);
        bool CheckUsernameExists(string username);
        bool IsBlacklisted(string ipAddress);
        int AddToBlacklist(BlacklistedAddress blacklistedAddress);
        void UpdateUser(User user);
        Task<string> GetUsernameByIdAsync(int userId);

        // Ban related methods
        Task<int> BanUserAsync(UserBan ban);
        Task<bool> UnbanUserAsync(int banId);
        Task<UserBan?> GetActiveBanByUserIdAsync(int userId);
        Task<UserBan?> GetActiveBanByIpAddressAsync(string ipAddress);
        Task<IEnumerable<UserBan>> GetBanHistoryByUserIdAsync(int userId);
        Task<IEnumerable<UserBan>> GetAllActiveBansAsync();
        Task<IEnumerable<UserBan>> GetAllBansAsync(string searchTerm = "", int page = 1, int pageSize = 20, string sortField = "", string sortDirection = "asc");
        Task<int> GetTotalBansCountAsync(string searchTerm = "");
        Task<UserBan?> GetBanByIdAsync(int banId);

        // Admin methods
        Task<IEnumerable<User>> GetUsersAsync(string searchTerm, int page, int pageSize, string sortField = "", string sortDirection = "asc");
        Task<int> GetTotalUsersCountAsync(string searchTerm);
        Task<bool> UpdateUserTierAsync(int userId, UserTier tier);
    }
}