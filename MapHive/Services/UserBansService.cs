namespace MapHive.Services
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using MapHive.Models.RepositoryModels;
    using MapHive.Repositories;

    public class AccountBansService(
        IAccountBansRepository accountBansRepository,
        IUserContextService userContextService) : IAccountBansService
    {
        private readonly IAccountBansRepository _accountBansRepository = accountBansRepository;
        private readonly IUserContextService _userContextService = userContextService;

        public Task<int> BanAccountAsync(int accountId, bool isPermanent, int? durationInDays, string? reason)
        {
            return _accountBansRepository.BanAccountAsync(new AccountBanCreate(){
                AccountId = accountId,
                BannedByAccountId = _userContextService.AccountIdRequired,
                BannedAt = DateTime.UtcNow,
                ExpiresAt = isPermanent ? null : DateTime.UtcNow.AddDays(durationInDays ?? 0),
                Reason = reason
            });
        }
    }
}
