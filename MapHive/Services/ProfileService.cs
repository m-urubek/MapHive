using MapHive.Models.RepositoryModels;
using MapHive.Models.ViewModels;
using MapHive.Repositories;

namespace MapHive.Services
{
    public class ProfileService : IProfileService
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapLocationRepository _mapRepository;
        private readonly IDiscussionRepository _discussionRepository;
        private readonly IUserContextService _userContext;

        public ProfileService(
            IUserRepository userRepository,
            IMapLocationRepository mapRepository,
            IDiscussionRepository discussionRepository,
            IUserContextService userContext)
        {
            this._userRepository = userRepository;
            this._mapRepository = mapRepository;
            this._discussionRepository = discussionRepository;
            this._userContext = userContext;
        }

        public async Task<PrivateProfileViewModel?> GetPrivateProfileAsync(int userId)
        {
            UserGet? user = await this._userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return null;
            }

            IEnumerable<MapLocationGet> locations = await this._mapRepository.GetLocationsByUserIdAsync(userId);
            IEnumerable<DiscussionThreadGet> threads = await this._discussionRepository.GetThreadsByUserIdAsync(userId);

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

        public async Task<PublicProfileViewModel?> GetPublicProfileAsync(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return null;
            }

            UserGet? userGet = await this._userRepository.GetUserByUsernameAsync(username);
            if (userGet == null)
            {
                return null;
            }

            int? currentUserId = this._userContext.UserId;
            _ = currentUserId.HasValue && userGet.Id == currentUserId.Value;

            // Check ban status
            UserBanGet? activeBan = await this._userRepository.GetActiveBanByUserIdAsync(userGet.Id);
            if (activeBan == null || !activeBan.IsActive)
            {
                string? registrationIp = userGet.IpAddressHistory?.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                if (!string.IsNullOrEmpty(registrationIp))
                {
                    activeBan = await this._userRepository.GetActiveBanByIpAddressAsync(registrationIp);
                }
            }

            IEnumerable<MapLocationGet> locations = await this._mapRepository.GetLocationsByUserIdAsync(userGet.Id);
            IEnumerable<DiscussionThreadGet> threads = await this._discussionRepository.GetThreadsByUserIdAsync(userGet.Id);

            bool isAdmin = false;
            if (currentUserId.HasValue)
            {
                UserGet? currentUser = await this._userRepository.GetUserByIdAsync(currentUserId.Value);
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