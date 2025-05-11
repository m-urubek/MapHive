namespace MapHive.Services
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using MapHive.Models.RepositoryModels;
    using MapHive.Repositories;

    public class UserBansService(
        IUserBansRepository userBansRepository) : IUserBansService
    {
        private readonly IUserBansRepository _userBansRepository = userBansRepository;

        public Task<int> BanUserAsync(UserBanGetCreate banDto)
        {
            return _userBansRepository.BanUserAsync(banDto);
        }

        public Task<bool> RemoveUserBanAsync(int banId)
        {
            return _userBansRepository.RemoveUserBanAsync(banId);
        }

        public Task<UserBanGet?> GetActiveBanByUserIdAsync(int userId)
        {
            return _userBansRepository.GetActiveBanByUserIdAsync(userId);
        }

        public Task<UserBanGet?> GetActiveBanByIpAddressAsync(string hashedIpAddress)
        {
            return _userBansRepository.GetActiveBanByIpAddressAsync(hashedIpAddress);
        }

        public Task<IEnumerable<UserBanGet>> GetAllActiveBansAsync()
        {
            return _userBansRepository.GetAllActiveBansAsync();
        }

        public Task<IEnumerable<UserBanGet>> GetAllBansAsync(
            string searchTerm = "",
            int page = 1,
            int pageSize = 20,
            string sortColumnName = "",
            string sortDirection = "asc")
        {
            return _userBansRepository.GetAllBansAsync(
                        searchTerm: searchTerm, page: page, pageSize: pageSize,
                        sortColumnName: sortColumnName, sortDirection: sortDirection);
        }
    }
}
