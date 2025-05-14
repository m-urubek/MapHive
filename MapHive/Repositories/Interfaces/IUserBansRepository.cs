namespace MapHive.Repositories
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using MapHive.Models.RepositoryModels;

    public interface IAccountBansRepository
    {
        Task<int> BanAccountAsync(AccountBanCreate banDto);
        Task<bool> RemoveAccountBanAsync(int banId);
        Task<AccountBanGet?> GetActiveAccountBanByAccountIdAsync(int accountId);
        Task<AccountBanGet?> GetActiveAccountBanByUsernameAsync(string username);
        Task<IEnumerable<AccountBanGet>> GetAllActiveBansAsync();
        Task<IEnumerable<AccountBanGet>> GetAllBansAsync(string searchTerm = "", int page = 1, int pageSize = 20, string sortColumnName = "", string sortDirection = "asc");
    }
}
