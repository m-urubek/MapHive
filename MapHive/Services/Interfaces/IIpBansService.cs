namespace MapHive.Services
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using MapHive.Models.RepositoryModels;

    public interface IIpBansService
    {
        Task<int> BanIpAddressAsync(string hashedIpAddress, bool isPermanent, int? durationInDays, string? reason);
    }
}
