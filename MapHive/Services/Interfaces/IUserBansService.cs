namespace MapHive.Services
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using MapHive.Models.RepositoryModels;

    public interface IAccountBansService
    {
        Task<int> BanAccountAsync(int accountId, bool isPermanent, int? durationInDays, string? reason);
    }
}
