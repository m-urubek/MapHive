namespace MapHive.Services
{
    using MapHive.Models.Enums;
    using MapHive.Models.RepositoryModels;
    using MapHive.Models.ViewModels;
    using MapHive.Repositories;

    public class ProfileService(
        IAccountsRepository userRepository,
        IMapLocationRepository mapRepository,
        IDiscussionRepository discussionRepository,
        IUserContextService userContextService,
        IAccountBansRepository AccountBansRepository,
        IAccountService accountService,
        IIpBansRepository _ipBansRepository) : IProfileService
    {
        private readonly IAccountsRepository _userRepository = userRepository;
        private readonly IMapLocationRepository _mapRepository = mapRepository;
        private readonly IDiscussionRepository _discussionRepository = discussionRepository;
        private readonly IUserContextService _userContextService = userContextService;
        private readonly IAccountBansRepository _AccountBansRepository = AccountBansRepository;
        private readonly IIpBansRepository _ipBansRepository = _ipBansRepository;
        private readonly IAccountService _accountService = accountService;

        private static readonly char[] separator = ['\n', '\r'];

        public async Task<PrivateProfileViewModel> GetPrivateProfileAsync()
        {
            AccountGet user = await _userRepository.GetAccountByIdOrThrowAsync(id: _userContextService.AccountIdRequired);
            IEnumerable<MapLocationGet> locations = await _mapRepository.GetLocationsByAccountIdAsync(accountId: _userContextService.AccountIdRequired);
            IEnumerable<DiscussionThreadGet> threads = await _discussionRepository.GetThreadsByAccountIdAsync(accountId: _userContextService.AccountIdRequired);

            return new PrivateProfileViewModel
            {
                AccountId = user.Id,
                Username = user.Username,
                Tier = user.Tier,
                RegistrationDate = user.RegistrationDate,
                UserLocations = locations,
                UserThreads = threads,
                ChangeUsernameViewModel = new ChangeUsernameViewModel { NewUsername = user.Username },
                ChangePasswordViewModel = new ChangePasswordViewModel()
            };
        }

        public async Task<PublicProfileViewModel> GetPublicProfileAsync(string username)
        {
            AccountGet accountGet = await _userRepository.GetAccountByUsernameOrThrowAsync(username: username);

            return await GetPublicProfileBaseAsync(accountGet: accountGet);
        }

        public async Task<PublicProfileViewModel> GetPublicProfileAsync(int accountId)
        {
            AccountGet accountGet = await _userRepository.GetAccountByIdOrThrowAsync(id: accountId);
            return await GetPublicProfileBaseAsync(accountGet: accountGet);
        }

        private async Task<PublicProfileViewModel> GetPublicProfileBaseAsync(AccountGet accountGet)
        {
            IEnumerable<MapLocationGet> locations = await _mapRepository.GetLocationsByAccountIdAsync(accountId: accountGet.Id);
            IEnumerable<DiscussionThreadGet> threads = await _discussionRepository.GetThreadsByAccountIdAsync(accountId: accountGet.Id);

            return new PublicProfileViewModel
            {
                AccountId = accountGet.Id,
                Username = accountGet.Username,
                Tier = accountGet.Tier,
                RegistrationDate = accountGet.RegistrationDate,
                UserLocations = locations,
                UserThreads = threads,
                CurrentBan = await _accountService.GetActiveBanViewModelAsync(accountId: accountGet.Id),
            };

        }
    }
}
