namespace MapHive.Services
{
    using AutoMapper;
    using MapHive.Models;
    using MapHive.Models.BusinessModels;
    using MapHive.Models.Enums;
    using MapHive.Models.Exceptions;
    using MapHive.Models.RepositoryModels;
    using MapHive.Models.ViewModels;
    using MapHive.Repositories;

    public class AccountService(
        IAuthService authService,
        ILogManagerService logManager,
        IAccountsRepository userRepository,
        IMapper mapper,
        IRequestContextService requestContextService,
        IAccountBansRepository accountBansRepository,
        IIpBansRepository ipBansRepository) : IAccountService
    {
        private readonly IAuthService _authService = authService;
        private readonly ILogManagerService _logManagerService = logManager;
        private readonly IAccountsRepository _userRepository = userRepository;
        private readonly IMapper _mapper = mapper;
        private readonly IRequestContextService _requestContextService = requestContextService;
        private readonly IAccountBansRepository _accountBansRepository = accountBansRepository;
        private readonly IIpBansRepository _ipBansRepository = ipBansRepository;

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrEmpty(value: _requestContextService.HashedIpAddress))
                throw new PublicWarningException("Unable to retrieve your IP address. Login cannot proceed.");

            AuthResponse response = await _authService.LoginAsync(request: request);
            _ = _logManagerService.LogAsync(severity: LogSeverity.Information, message: $"User {request.Username} successfully logged in.");
            return response;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrEmpty(value: _requestContextService.HashedIpAddress))
                throw new PublicWarningException("Unable to retrieve your IP address. Login cannot proceed.");

            AuthResponse response = await _authService.RegisterAsync(request: request);
            _ = _logManagerService.LogAsync(severity: LogSeverity.Information, message: $"User {request.Username} successfully registered and logged in.");
            return response;
        }

        public Task LogoutAsync()
        {
            _ = _logManagerService.LogAsync(severity: LogSeverity.Information, message: "User logged out.");
            return _authService.LogoutAsync();
        }

        public bool VerifyPassword(string password, string storedHash)
        {
            return _authService.VerifyPassword(password: password, storedHash: storedHash);
        }

        public string HashPassword(string password)
        {
            return _authService.HashPassword(password: password);
        }

        public async Task ChangeUsernameAsync(int accountId, string newUsername)
        {
            // Retrieve user
            AccountGet? accountGet = await _userRepository.GetAccountByIdAsync(id: accountId) ?? throw new RedUserException("User not found");

            // Check username availability
            if (await _userRepository.CheckUsernameExistsAsync(username: newUsername)
                && !accountGet.Username.Equals(value: newUsername, comparisonType: StringComparison.OrdinalIgnoreCase))
            {
                throw new OrangeUserException("Username already exists");
            }

            // Update username
            accountGet.Username = newUsername;
            UserUpdate updateDto = _mapper.Map<UserUpdate>(source: accountGet);
            _ = await _userRepository.UpdateAccountAsync(updateDto: updateDto);
        }

        public async Task ChangePasswordAsync(int accountId, string currentPassword, string newPassword)
        {
            // Retrieve user
            AccountGet? accountGet = await _userRepository.GetAccountByIdAsync(id: accountId) ?? throw new RedUserException("User not found");

            // Verify current password
            if (!VerifyPassword(password: currentPassword, storedHash: accountGet.PasswordHash))
            {
                throw new OrangeUserException("Current password is incorrect");
            }

            // Update password hash
            UserUpdate updateDto = _mapper.Map<UserUpdate>(source: accountGet);
            updateDto.PasswordHash = HashPassword(password: newPassword);
            _ = await _userRepository.UpdateAccountAsync(updateDto: updateDto);
        }

        public async Task<BanViewModel?> GetActiveBanViewModelAsync(int accountId)
        {
            BanViewModel? banViewModel = null;
            AccountBanGet? AccountBan = await _accountBansRepository.GetActiveAccountBanByAccountIdAsync(accountId: accountId);
            if (AccountBan != null)
            {
                banViewModel = _mapper.Map<BanViewModel>(AccountBan);
                banViewModel.BanType = BanType.Account;
            }
            else
            {
                IpBanGet? IpBan = await _ipBansRepository.GetActiveIpBanByIpAddressAsync(hashedIpAddress: _requestContextService.HashedIpAddress);
                if (IpBan != null)
                {
                    banViewModel = _mapper.Map<BanViewModel>(IpBan);
                    banViewModel.BanType = BanType.IpAddress;
                }
            }
            return banViewModel;
        }

        /// <summary>
        /// Removes the active ban for the specified account ID or, if none, the active IP ban for the current request.
        /// </summary>
        public async Task<bool> UnbanAccountAsync(int accountId)
        {
            // Try to remove an active account ban first
            AccountBanGet? activeAccountBan = await _accountBansRepository.GetActiveAccountBanByAccountIdAsync(accountId: accountId);
            if (activeAccountBan != null)
            {
                return await _accountBansRepository.RemoveAccountBanAsync(banId: activeAccountBan.Id);
            }

            // If no account ban, try to remove an active IP ban for this request
            var activeIpBan = await _ipBansRepository.GetActiveIpBanByIpAddressAsync(hashedIpAddress: _requestContextService.HashedIpAddress);
            if (activeIpBan != null)
            {
                return await _ipBansRepository.RemoveIpBanAsync(banId: activeIpBan.Id);
            }

            return false;
        }
    }
}
