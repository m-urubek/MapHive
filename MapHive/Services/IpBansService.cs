namespace MapHive.Services
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using MapHive.Models.RepositoryModels;
    using MapHive.Repositories;

    public class IpBansService(
        IIpBansRepository ipBansRepository) : IIpBansService
    {
        private readonly IIpBansRepository _ipBansRepository = ipBansRepository;

        public Task<bool> IsIpBannedAsync(string hashedIpAddress)
        {
            return _ipBansRepository.IsIpBannedAsync(hashedIpAddress);
        }

        public Task<int> CreateIpBanAsync(IpBanCreate ipBan)
        {
            return _ipBansRepository.CreateIpBanAsync(ipBan);
        }

        public Task<bool> RemoveIpBanAsync(int banId)
        {
            return _ipBansRepository.RemoveIpBanAsync(banId);
        }

        public Task<IpBanGet?> GetActiveIpBanByIpAddressAsync(string hashedIpAddress)
        {
            return _ipBansRepository.GetActiveIpBanByIpAddressAsync(hashedIpAddress);
        }

        public Task<IEnumerable<IpBanGet>> GetAllActiveIpBansAsync()
        {
            return _ipBansRepository.GetAllActiveIpBansAsync();
        }

        public Task<IEnumerable<IpBanGet>> GetAllIpBansAsync(
            string searchTerm = "",
            int page = 1,
            int pageSize = 20,
            string sortColumnName = "",
            string sortDirection = "asc")
        {
            return _ipBansRepository.GetAllIpBansAsync(
                        searchTerm: searchTerm, page: page, pageSize: pageSize,
                        sortColumnName: sortColumnName, sortDirection: sortDirection);
        }
    }
}
