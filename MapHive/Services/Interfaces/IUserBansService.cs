namespace MapHive.Services;

using System.Threading.Tasks;

public interface IAccountBansService
{
    Task<int> BanAccountAsync(int accountId, bool isPermanent, int? durationInDays, string? reason);
}
