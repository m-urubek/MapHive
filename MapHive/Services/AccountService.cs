using AutoMapper;
using MapHive.Models;
using MapHive.Models.BusinessModels;
using MapHive.Models.Enums;
using MapHive.Models.Exceptions;
using MapHive.Models.RepositoryModels;
using MapHive.Repositories;

namespace MapHive.Services
{
    public class AccountService : IAccountService
    {
        private readonly IAuthService _authService;
        private readonly ILogManagerService _logManagerService;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly IRequestContextService _requestContextService;

        public AccountService(
            IAuthService authService,
            ILogManagerService logManager,
            IUserRepository userRepository,
            IMapper mapper,
            IRequestContextService requestContextService)
        {
            this._authService = authService;
            this._logManagerService = logManager;
            this._userRepository = userRepository;
            this._mapper = mapper;
            this._requestContextService = requestContextService;
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrEmpty(_requestContextService.IpAddress))
            {
                this._logManagerService.Log(LogSeverity.Warning, "Login failed: Unable to retrieve client IP address.");
                throw new OrangeUserException("Login failed due to a network issue. Please try again.");
            }

            AuthResponse response = await this._authService.LoginAsync(request, _requestContextService.IpAddress);
            this._logManagerService.Log(LogSeverity.Information, $"User {request.Username} successfully logged in.");
            return response;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrEmpty(_requestContextService.IpAddress))
            {
                this._logManagerService.Log(LogSeverity.Error, "Registration failed: Unable to retrieve client IP address.");
                throw new RedUserException("Unable to retrieve your IP address. Registration cannot proceed.");
            }

            AuthResponse response = await this._authService.RegisterAsync(request, _requestContextService.IpAddress);
            this._logManagerService.Log(LogSeverity.Information, $"User {request.Username} successfully registered and logged in.");
            return response;
        }

        public Task LogoutAsync()
        {
            this._logManagerService.Log(LogSeverity.Information, "User logged out.");
            return this._authService.LogoutAsync();
        }

        public bool VerifyPassword(string password, string storedHash)
        {
            return this._authService.VerifyPassword(password, storedHash);
        }

        public string HashPassword(string password)
        {
            return this._authService.HashPassword(password);
        }

        public async Task ChangeUsernameAsync(int userId, string newUsername)
        {
            // Retrieve user
            UserGet? userGet = await this._userRepository.GetUserByIdAsync(userId);
            if (userGet == null)
            {
                throw new RedUserException("User not found");
            }

            // Check username availability
            if (await this._userRepository.CheckUsernameExistsAsync(newUsername)
                && !userGet.Username.Equals(newUsername, StringComparison.OrdinalIgnoreCase))
            {
                throw new OrangeUserException("Username already exists");
            }

            // Update username
            userGet.Username = newUsername;
            UserUpdate updateDto = this._mapper.Map<UserUpdate>(userGet);
            _ = await this._userRepository.UpdateUserAsync(updateDto);
        }

        public async Task ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            // Retrieve user
            UserGet? userGet = await this._userRepository.GetUserByIdAsync(userId);
            if (userGet == null)
            {
                throw new RedUserException("User not found");
            }

            // Verify current password
            if (!this.VerifyPassword(currentPassword, userGet.PasswordHash))
            {
                throw new OrangeUserException("Current password is incorrect");
            }

            // Update password hash
            UserUpdate updateDto = this._mapper.Map<UserUpdate>(userGet);
            updateDto.PasswordHash = this.HashPassword(newPassword);
            _ = await this._userRepository.UpdateUserAsync(updateDto);
        }
    }
}