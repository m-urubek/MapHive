namespace MapHive.Repositories;

using System.Threading.Tasks;
using MapHive.Models.Data.DbTableModels;

public interface IAccountBansRepository
{
    Task<int> CreateAccountBanAsync(int accountId, int bannedByAccountId, string? reason, DateTime banCreatedDateTime, DateTime? expiresAt);
    Task RemoveAccountBanAsync(int banId);
    Task<AccountBanExtended?> GetActiveAccountBanByAccountIdAsync(int accountId);
    Task<AccountBanExtended?> GetActiveAccountBanByUsernameAsync(string username);
}
