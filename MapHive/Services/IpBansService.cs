namespace MapHive.Services
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using MapHive.Models.RepositoryModels;
    using MapHive.Repositories;

    public class IpBansService(
        IIpBansRepository ipBansRepository,
        IUserContextService userContextService) : IIpBansService
    {
        private readonly IIpBansRepository _ipBansRepository = ipBansRepository;
        private readonly IUserContextService _userContextService = userContextService;

        public Task<int> BanIpAddressAsync(string hashedIpAddress, bool isPermanent, int? durationInDays, string? reason)
        {
            return _ipBansRepository.CreateIpBanAsync(new IpBanCreate(){
                HashedIpAddress = hashedIpAddress,
                BannedByAccountId = _userContextService.AccountIdRequired,
                BannedAt = DateTime.UtcNow,
                ExpiresAt = isPermanent ? null : DateTime.UtcNow.AddDays(durationInDays ?? 0),
                Reason = reason
            });
        }
    }
}
