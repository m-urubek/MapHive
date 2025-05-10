namespace MapHive.Services
{
    using MapHive.Models.RepositoryModels;
    using MapHive.Models.ViewModels;
    using MapHive.Repositories;

    public class ProfileService(
        IUserRepository userRepository,
        IMapLocationRepository mapRepository,
        IDiscussionRepository discussionRepository,
        IUserContextService userContextService) : IProfileService
    {
        private readonly IUserRepository _userRepository = userRepository;
        private readonly IMapLocationRepository _mapRepository = mapRepository;
        private readonly IDiscussionRepository _discussionRepository = discussionRepository;
        private readonly IUserContextService _userContextService = userContextService;
        private static readonly char[] separator = ['\n', '\r'];

        public async Task<PrivateProfileViewModel> GetPrivateProfileAsync(int userId)
        {
            UserGet? user = await _userRepository.GetUserByIdAsync(id: userId) ?? throw new Exception(message: $"User \"{userId}\" not found!");
            IEnumerable<MapLocationGet> locations = await _mapRepository.GetLocationsByUserIdAsync(userId: userId);
            IEnumerable<DiscussionThreadGet> threads = await _discussionRepository.GetThreadsByUserIdAsync(userId: userId);

            return new PrivateProfileViewModel
            {
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
            UserGet userGet = await _userRepository.GetUserByUsernameAsync(username: username) ?? throw new Exception($"User \"{username}\" not found!");

            int? currentUserId = _userContextService.UserId;
            _ = currentUserId.HasValue && userGet.Id == currentUserId.Value;

            // Check ban status
            UserBanGet? activeBan = await _userRepository.GetActiveBanByUserIdAsync(userId: userGet.Id);
            if (activeBan == null || !activeBan.IsActive)
            {
                string? registrationIp = userGet.IpAddressHistory?.Split(separator: separator, options: StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                if (!string.IsNullOrEmpty(value: registrationIp))
                {
                    activeBan = await _userRepository.GetActiveBanByIpAddressAsync(hashedIpAddress: registrationIp);
                }
            }

            IEnumerable<MapLocationGet> locations = await _mapRepository.GetLocationsByUserIdAsync(userId: userGet.Id);
            IEnumerable<DiscussionThreadGet> threads = await _discussionRepository.GetThreadsByUserIdAsync(userId: userGet.Id);

            bool isAdmin = false;
            if (currentUserId.HasValue)
            {
                UserGet? currentUser = await _userRepository.GetUserByIdAsync(id: currentUserId.Value);
                isAdmin = currentUser != null && currentUser.Tier == Models.Enums.UserTier.Admin;
            }

            return new PublicProfileViewModel
            {
                UserId = userGet.Id,
                Username = userGet.Username,
                Tier = userGet.Tier,
                RegistrationDate = userGet.RegistrationDate,
                UserLocations = locations,
                UserThreads = threads,
                IsAdmin = isAdmin,
                CurrentBan = activeBan
            };
        }
    }
}