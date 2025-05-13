namespace MapHive.Services
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using MapHive.Models.RepositoryModels;

    public interface IIpBansService
    {
        Task<bool> IsIpBannedAsync(string hashedIpAddress);
        Task<int> CreateIpBanAsync(IpBanCreate ipBan);
        Task<bool> RemoveIpBanAsync(int banId);
        Task<IpBanGet?> GetActiveIpBanByIpAddressAsync(string hashedIpAddress);
        Task<IEnumerable<IpBanGet>> GetAllActiveIpBansAsync();
        Task<IEnumerable<IpBanGet>> GetAllIpBansAsync(string searchTerm = "", int page = 1, int pageSize = 20, string sortColumnName = "", string sortDirection = "asc");
    }
}
