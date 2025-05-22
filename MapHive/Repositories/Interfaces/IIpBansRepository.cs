namespace MapHive.Repositories;

using System.Threading.Tasks;
using MapHive.Models.Data.DbTableModels;

public interface IIpBansRepository
{
    Task<bool> IsIpBannedAsync(string hashedIpAddress);
    Task<int> CreateIpBanAsync(string hashedIpAddress, string? reason, DateTime banCreatedDateTime, int bannedByAccountId, DateTime? expiresAt);
    Task RemoveIpBanAsync(int banId);
    Task<IpBanExtended?> GetActiveIpBanByIpAddressAsync(string hashedIpAddress);
}
