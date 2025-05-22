namespace MapHive.Services;

using System.Threading.Tasks;

public interface IIpBansService
{
    Task<int> BanIpAddressAsync(string hashedIpAddress, bool isPermanent, int? durationInDays, string? reason);
}
