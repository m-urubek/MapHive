using AutoMapper;
using MapHive.Models;
using MapHive.Models.BusinessModels;
using MapHive.Models.Enums;
using MapHive.Models.Exceptions;
using MapHive.Models.RepositoryModels;
using MapHive.Repositories;
using MapHive.Utilities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace MapHive.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogManagerService _logManagerService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMapper _mapper;

        public AuthService(IUserRepository userRepository,
            ILogManagerService logManager,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper)
        {
            this._userRepository = userRepository;
            this._logManagerService = logManager;
            this._httpContextAccessor = httpContextAccessor;
            this._mapper = mapper;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request, string ipAddress)
        {
            // Hash IP for checking ban and storing
            string hashedIpAddress = NetworkingUtility.HashIpAddress(ipAddress);

            // Check if username already exists
            if (await this._userRepository.CheckUsernameExistsAsync(request.Username))
            {
                throw new OrangeUserException("Username already exists");
            }

            // Check if IP is banned
            if (await this._userRepository.IsIpBannedAsync(hashedIpAddress))
            {
                // LogGet the attempt but throw a user-friendly (less informative) error
                this._logManagerService.Log(LogSeverity.Warning, $"Registration attempt from banned IP: {ipAddress} (Hashed: {hashedIpAddress})");
                throw new OrangeUserException("Registration failed due to security reasons.");
            }

            // Create the user using DTO
            UserCreate userCreate = new()
            {
                Username = request.Username,
                PasswordHash = this.HashPassword(request.Password),
                RegistrationDate = DateTime.UtcNow,
                Tier = UserTier.Normal,
                IpAddressHistory = hashedIpAddress
            };
            int userId = await this._userRepository.CreateUserAsync(userCreate);

            // Retrieve the created user
            UserGet? userGet = await this._userRepository.GetUserByIdAsync(userId);
            if (userGet == null)
            {
                throw new InvalidOperationException("Unable to retrieve newly created user.");
            }

            // Automatically log in the user after registration
            await this.SignInUserAsync(this._mapper.Map<UserLogin>(userGet));

            // Log registration, including hashed IP after sign-in so UserId is available
            this._logManagerService.Log(LogSeverity.Information, $"New user registered: {userId}",
                additionalData: $"Hashed IP: {hashedIpAddress}");

            return new AuthResponse
            {
                Success = true,
                Message = "Registration successful",
                User = userGet // Return the created user object
            };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request, string ipAddress)
        {
            // Get user by username
            UserGet? userGet = await this._userRepository.GetUserByUsernameAsync(request.Username);
            if (userGet == null)
            {
                this._logManagerService.Log(LogSeverity.Warning, $"Failed login attempt for username: {request.Username} from IP: {ipAddress}");
                throw new OrangeUserException("Invalid username or password");
            }

            // Check password
            if (!this.VerifyPassword(request.Password, userGet.PasswordHash))
            {
                this._logManagerService.Log(LogSeverity.Warning, $"Failed login attempt for username: {request.Username} from IP: {ipAddress}");
                throw new OrangeUserException("Invalid username or password");
            }

            // Check if the user is banned
            UserBanGet? activeBan = await this._userRepository.GetActiveBanByUserIdAsync(userGet.Id);
            if (activeBan != null)
            {
                this._logManagerService.Log(LogSeverity.Warning, $"Banned user login attempt: {request.Username} (Ban ID: {activeBan.Id})");
                string banMessage = "Your account is currently banned.";
                if (activeBan.ExpiresAt.HasValue)
                {
                    banMessage += $" Ban expires on: {activeBan.ExpiresAt.Value:yyyy-MM-dd HH:mm} UTC.";
                }
                if (!string.IsNullOrWhiteSpace(activeBan.Reason))
                {
                    banMessage += $" Reason: {activeBan.Reason}";
                }
                throw new RedUserException(banMessage); // Use RedUserException for bans
            }

            // Hash the current IP and check if it's banned
            string hashedIpAddress = NetworkingUtility.HashIpAddress(ipAddress);
            if (await this._userRepository.IsIpBannedAsync(hashedIpAddress))
            {
                this._logManagerService.Log(LogSeverity.Warning, $"Login attempt from banned IP: {ipAddress} (Hashed: {hashedIpAddress}) for user: {request.Username}");
                throw new OrangeUserException("Login failed due to security reasons.");
            }

            // Sign in the user before logging so HttpContext.User includes the ID
            await this.SignInUserAsync(this._mapper.Map<UserLogin>(userGet));

            // Log successful login after sign-in
            this._logManagerService.Log(LogSeverity.Information, $"UserLogin logged in: {request.Username}", additionalData: $"IP: {ipAddress}");

            // Update IP history if necessary (consider rate limiting or specific logic)
            if (userGet.IpAddressHistory == null || !userGet.IpAddressHistory.Contains(hashedIpAddress))
            {
                // Update user via UserUpdate DTO
                UserUpdate updateDto = new()
                {
                    Id = userGet.Id,
                    Username = userGet.Username,
                    PasswordHash = userGet.PasswordHash,
                    Tier = userGet.Tier,
                    IpAddressHistory = userGet.IpAddressHistory
                };
                _ = await this._userRepository.UpdateUserAsync(updateDto);
            }

            return new AuthResponse
            {
                Success = true,
                Message = "Login successful",
                User = userGet
            };
        }

        public async Task LogoutAsync()
        {
            if (this._httpContextAccessor.HttpContext != null)
            {
                await this._httpContextAccessor.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                this._logManagerService.Log(LogSeverity.Information, "UserLogin logged out");
            }
        }

        public string HashPassword(string password)
        {
            // Normalize the password to ensure consistent hashing
            password = password.Normalize(System.Text.NormalizationForm.FormKD);

            using SHA256 sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));

            // Use lowercase hex format for consistency
            StringBuilder builder = new();
            for (int i = 0; i < bytes.Length; i++)
            {
                _ = builder.Append(bytes[i].ToString("x2"));
            }

            // Return lowercase hex string
            return builder.ToString().ToLowerInvariant();
        }

        public bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
            {
                return false;
            }
            string hashedPassword = this.HashPassword(password);
            // Use case-insensitive comparison for hashes
            return string.Equals(hashedPassword, storedHash, StringComparison.OrdinalIgnoreCase);
        }

        // Helper method to sign in the user
        private async Task SignInUserAsync(UserLogin user)
        {
            List<Claim> claims = new()
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Role, user.Tier.ToString()), // Add tier number as role
                new(ClaimTypes.Role, ((UserTier)user.Tier).ToString()) // Add tier name as role
            };

            ClaimsIdentity claimsIdentity = new(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            AuthenticationProperties authProperties = new()
            {
                AllowRefresh = true,
                // Refreshing the authentication session should be allowed.

                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7),
                // The time at which the authentication ticket expires. A
                // value set here overrides the ExpireTimeSpan option of
                // CookieAuthenticationOptions set with AddCookie.

                IsPersistent = true,
                // Whether the authentication session is persisted across
                // multiple requests. When used with cookies, controls
                // whether the cookie's lifetime is absolute (matching the
                // lifetime of the authentication ticket) or session-based.

                //IssuedUtc = <DateTimeOffset>,
                // The time at which the authentication ticket was issued.

                //RedirectUri = <string>
                // The full path or absolute URI to be used as an http
                // redirect response value.
            };

            if (this._httpContextAccessor.HttpContext != null)
            {
                await this._httpContextAccessor.HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);
            }
            else
            {
                this._logManagerService.Log(LogSeverity.Error, "HttpContext is null. Cannot sign in user.");
                // Handle the error appropriately, maybe throw an exception
                throw new InvalidOperationException("HttpContext is not available to sign in the user.");
            }
        }
    }
}