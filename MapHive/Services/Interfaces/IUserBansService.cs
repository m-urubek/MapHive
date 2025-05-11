namespace MapHive.Services
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using MapHive.Models.RepositoryModels;

    public interface IUserBansService
    {
        Task<int> BanUserAsync(UserBanGetCreate banDto);
        Task<bool> RemoveUserBanAsync(int banId);
        Task<UserBanGet?> GetActiveBanByUserIdAsync(int userId);
        Task<UserBanGet?> GetActiveBanByIpAddressAsync(string hashedIpAddress);
        Task<IEnumerable<UserBanGet>> GetAllActiveBansAsync();
        Task<IEnumerable<UserBanGet>> GetAllBansAsync(string searchTerm = "", int page = 1, int pageSize = 20, string sortColumnName = "", string sortDirection = "asc");
    }
}
