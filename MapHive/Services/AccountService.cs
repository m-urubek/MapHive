namespace MapHive.Services
{
    using AutoMapper;
    using MapHive.Models;
    using MapHive.Models.BusinessModels;
    using MapHive.Models.Enums;
    using MapHive.Models.Exceptions;
    using MapHive.Models.RepositoryModels;
    using MapHive.Repositories;

    public class AccountService(
        IAuthService authService,
        ILogManagerService logManager,
        IUserRepository userRepository,
        IMapper mapper,
        IRequestContextService requestContextService) : IAccountService
    {
        private readonly IAuthService _authService = authService;
        private readonly ILogManagerService _logManagerService = logManager;
        private readonly IUserRepository _userRepository = userRepository;
        private readonly IMapper _mapper = mapper;
        private readonly IRequestContextService _requestContextService = requestContextService;

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrEmpty(value: _requestContextService.IpAddress))
                throw new PublicWarningException("Unable to retrieve your IP address. Login cannot proceed.");

            AuthResponse response = await _authService.LoginAsync(request: request, ipAddress: _requestContextService.IpAddress);
            _ = _logManagerService.LogAsync(severity: LogSeverity.Information, message: $"User {request.Username} successfully logged in.");
            return response;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrEmpty(value: _requestContextService.IpAddress))
                throw new PublicWarningException("Unable to retrieve your IP address. Login cannot proceed.");

            AuthResponse response = await _authService.RegisterAsync(request: request, ipAddress: _requestContextService.IpAddress);
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

        public async Task ChangeUsernameAsync(int userId, string newUsername)
        {
            // Retrieve user
            UserGet? userGet = await _userRepository.GetUserByIdAsync(id: userId) ?? throw new RedUserException("User not found");

            // Check username availability
            if (await _userRepository.CheckUsernameExistsAsync(username: newUsername)
                && !userGet.Username.Equals(value: newUsername, comparisonType: StringComparison.OrdinalIgnoreCase))
            {
                throw new OrangeUserException("Username already exists");
            }

            // Update username
            userGet.Username = newUsername;
            UserUpdate updateDto = _mapper.Map<UserUpdate>(source: userGet);
            _ = await _userRepository.UpdateUserAsync(updateDto: updateDto);
        }

        public async Task ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            // Retrieve user
            UserGet? userGet = await _userRepository.GetUserByIdAsync(id: userId) ?? throw new RedUserException("User not found");

            // Verify current password
            if (!VerifyPassword(password: currentPassword, storedHash: userGet.PasswordHash))
            {
                throw new OrangeUserException("Current password is incorrect");
            }

            // Update password hash
            UserUpdate updateDto = _mapper.Map<UserUpdate>(source: userGet);
            updateDto.PasswordHash = HashPassword(password: newPassword);
            _ = await _userRepository.UpdateUserAsync(updateDto: updateDto);
        }
    }
}
