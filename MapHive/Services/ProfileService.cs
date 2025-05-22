namespace MapHive.Services;

using MapHive.Models.Data.DbTableModels;
using MapHive.Models.PageModels;
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

    public async Task<PrivateProfilePageModel> GetPrivateProfilePageModelAsync()
    {
        AccountAtomic user = await _userRepository.GetAccountByIdOrThrowAsync(id: _userContextService.AccountIdOrThrow);
        IEnumerable<LocationExtended> locations = await _mapRepository.GetLocationsByOwnerIdAsync(accountId: _userContextService.AccountIdOrThrow);
        IEnumerable<ThreadInitialMessageDbModel> threads = await _discussionRepository.GetInitialMessageThreadsByAccountIdAsync(accountId: _userContextService.AccountIdOrThrow);

        return new PrivateProfilePageModel
        {
            AccountId = _userContextService.AccountIdOrThrow,
            Username = user.Username,
            Tier = user.Tier,
            RegistrationDate = user.RegistrationDate,
            UserLocations = locations,
            UserThreads = threads,
            ChangeUsernamePageModel = new ChangeUsernamePageModel { NewUsername = user.Username },
            ChangePasswordPageModel = new ChangePasswordPageModel(),
            CurrentBan = await _accountService.GetActiveBanPageModelAsync(accountId: _userContextService.AccountIdOrThrow),
        };
    }

    public async Task<PublicProfilePageModel> GetPublicProfileAsync(string username)
    {
        AccountAtomic accountGet = await _userRepository.GetAccountByUsernameOrThrowAsync(username: username);

        return await GetPublicProfileBaseAsync(accountGet: accountGet);
    }

    public async Task<PublicProfilePageModel> GetPublicProfileAsync(int accountId)
    {
        AccountAtomic accountGet = await _userRepository.GetAccountByIdOrThrowAsync(id: accountId);
        return await GetPublicProfileBaseAsync(accountGet: accountGet);
    }

    private async Task<PublicProfilePageModel> GetPublicProfileBaseAsync(AccountAtomic accountGet)
    {
        IEnumerable<LocationExtended> locations = await _mapRepository.GetLocationsByOwnerIdAsync(accountId: accountGet.Id);
        IEnumerable<ThreadInitialMessageDbModel> threads = await _discussionRepository.GetInitialMessageThreadsByAccountIdAsync(accountId: accountGet.Id);

        return new PublicProfilePageModel
        {
            AccountId = accountGet.Id,
            Username = accountGet.Username,
            Tier = accountGet.Tier,
            RegistrationDate = accountGet.RegistrationDate,
            UserLocations = locations,
            UserThreads = threads,
            CurrentBan = await _accountService.GetActiveBanPageModelAsync(accountId: accountGet.Id),
            SignedInUserIsAdmin = _userContextService.IsAuthenticated && _userContextService.IsAdminOrThrow,
        };

    }
}
