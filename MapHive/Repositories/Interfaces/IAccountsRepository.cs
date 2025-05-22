namespace MapHive.Repositories;

using MapHive.Models.Data;
using MapHive.Models.Data.DbTableModels;
using MapHive.Models.Enums;

public interface IAccountsRepository
{
    public Task<int> CreateAccountAsync(
        string username,
        string passwordHash,
        DateTime registrationDate,
        AccountTier tier,
        string ipAddressHistory
    );

    // Basic user lookup
    Task<AccountAtomic?> GetAccountByUsernameAsync(string username);
    Task<AccountAtomic> GetAccountByUsernameOrThrowAsync(string username);
    Task<bool> CheckUsernameExistsAsync(string username);
    Task<AccountAtomic?> GetAccountByIdAsync(int id);
    Task<AccountAtomic> GetAccountByIdOrThrowAsync(int id);

    // Admin methods
    Task<IEnumerable<AccountAtomic>> GeAccountsAsync(string searchTerm, int page, int pageSize, string sortColumnName = "", string sortDirection = "asc");
    Task<int> GetTotalAccountsCountAsync(string searchTerm);
    Task UpdateAccountOrThrowAsync(
        int id,
        DynamicValue<string> username,
        DynamicValue<string> passwordHash,
        DynamicValue<AccountTier> tier,
        DynamicValue<string> ipAddressHistory);
    Task UpdateDarkModePreferenceAsync(int id, bool enabled);
}
