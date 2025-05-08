using AutoMapper;
using MapHive.Models;
using MapHive.Models.BusinessModels;
using MapHive.Models.Exceptions;
using MapHive.Models.RepositoryModels;
using MapHive.Repositories;
using MapHive.Singletons;

namespace MapHive.Services
{
    public class AccountService : IAccountService
    {
        private readonly IAuthService _authService;
        private readonly ILogManagerSingleton _logManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public AccountService(
            IAuthService authService,
            ILogManagerSingleton logManager,
            IHttpContextAccessor httpContextAccessor,
            IUserRepository userRepository,
            IMapper mapper)
        {
            this._authService = authService;
            this._logManager = logManager;
            this._httpContextAccessor = httpContextAccessor;
            this._userRepository = userRepository;
            this._mapper = mapper;
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            string? ipAddress = this._httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
            if (string.IsNullOrEmpty(ipAddress))
            {
                this._logManager.Warning("Login failed: Unable to retrieve client IP address.");
                throw new OrangeUserException("Login failed due to a network issue. Please try again.");
            }

            AuthResponse response = await this._authService.LoginAsync(request, ipAddress);
            this._logManager.Information($"User {request.Username} successfully logged in.");
            return response;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            string? ipAddress = this._httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
            if (string.IsNullOrEmpty(ipAddress))
            {
                this._logManager.Error("Registration failed: Unable to retrieve client IP address.");
                throw new RedUserException("Unable to retrieve your IP address. Registration cannot proceed.");
            }

            AuthResponse response = await this._authService.RegisterAsync(request, ipAddress);
            this._logManager.Information($"User {request.Username} successfully registered and logged in.");
            return response;
        }

        public Task LogoutAsync()
        {
            this._logManager.Information("User logged out.");
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