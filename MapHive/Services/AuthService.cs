namespace MapHive.Services
{
    using System.Security.Claims;
    using System.Security.Cryptography;
    using System.Text;
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

    public class AuthService(IUserRepository userRepository,
        ILogManagerService logManager,
        IHttpContextAccessor httpContextAccessor,
        IMapper mapper) : IAuthService
    {
        private readonly IUserRepository _userRepository = userRepository;
        private readonly ILogManagerService _logManagerService = logManager;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
        private readonly IMapper _mapper = mapper;

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request, string ipAddress)
        {
            // Hash IP for checking ban and storing
            string hashedIpAddress = NetworkingUtility.HashIpAddress(ipAddress: ipAddress);

            // Check if username already exists
            if (await _userRepository.CheckUsernameExistsAsync(username: request.Username))
            {
                throw new OrangeUserException("Username already exists");
            }

            // Check if IP is banned
            if (await _userRepository.IsIpBannedAsync(hashedIpAddress: hashedIpAddress))
            {
                // LogGet the attempt but throw a user-friendly (less informative) error
                _logManagerService.Log(severity: LogSeverity.Warning, message: $"Registration attempt from banned IP: {ipAddress} (Hashed: {hashedIpAddress})");
                throw new OrangeUserException("Registration failed due to security reasons.");
            }

            // Create the user using DTO
            UserCreate userCreate = new()
            {
                Username = request.Username,
                PasswordHash = HashPassword(password: request.Password),
                RegistrationDate = DateTime.UtcNow,
                Tier = UserTier.Normal,
                IpAddressHistory = hashedIpAddress
            };
            int userId = await _userRepository.CreateUserAsync(createDto: userCreate);

            // Retrieve the created user
            UserGet? userGet = await _userRepository.GetUserByIdAsync(id: userId) ?? throw new InvalidOperationException("Unable to retrieve newly created user.");

            // Automatically log in the user after registration
            await SignInUserAsync(user: _mapper.Map<UserLogin>(source: userGet));

            // Log registration, including hashed IP after sign-in so UserId is available
            _logManagerService.Log(severity: LogSeverity.Information, message: $"New user registered: {userId}",
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
            UserGet? userGet = await _userRepository.GetUserByUsernameAsync(username: request.Username);
            if (userGet == null)
            {
                _logManagerService.Log(severity: LogSeverity.Warning, message: $"Failed login attempt for username: {request.Username} from IP: {ipAddress}");
                throw new OrangeUserException("Invalid username or password");
            }

            // Check password
            if (!VerifyPassword(password: request.Password, storedHash: userGet.PasswordHash))
            {
                _logManagerService.Log(severity: LogSeverity.Warning, message: $"Failed login attempt for username: {request.Username} from IP: {ipAddress}");
                throw new OrangeUserException("Invalid username or password");
            }

            // Check if the user is banned
            UserBanGet? activeBan = await _userRepository.GetActiveBanByUserIdAsync(userId: userGet.Id);
            if (activeBan != null)
            {
                _logManagerService.Log(severity: LogSeverity.Warning, message: $"Banned user login attempt: {request.Username} (Ban ID: {activeBan.Id})");
                string banMessage = "Your account is currently banned.";
                if (activeBan.ExpiresAt.HasValue)
                {
                    banMessage += $" Ban expires on: {activeBan.ExpiresAt.Value:yyyy-MM-dd HH:mm} UTC.";
                }
                if (!string.IsNullOrWhiteSpace(value: activeBan.Reason))
                {
                    banMessage += $" Reason: {activeBan.Reason}";
                }
                throw new RedUserException(banMessage); // Use RedUserException for bans
            }

            // Hash the current IP and check if it's banned
            string hashedIpAddress = NetworkingUtility.HashIpAddress(ipAddress: ipAddress);
            if (await _userRepository.IsIpBannedAsync(hashedIpAddress: hashedIpAddress))
            {
                _logManagerService.Log(severity: LogSeverity.Warning, message: $"Login attempt from banned IP: {ipAddress} (Hashed: {hashedIpAddress}) for user: {request.Username}");
                throw new OrangeUserException("Login failed due to security reasons.");
            }

            // Sign in the user before logging so HttpContext.User includes the ID
            await SignInUserAsync(user: _mapper.Map<UserLogin>(source: userGet));

            // Log successful login after sign-in
            _logManagerService.Log(severity: LogSeverity.Information, message: $"UserLogin logged in: {request.Username}", additionalData: $"IP: {ipAddress}");

            // Update IP history if necessary (consider rate limiting or specific logic)
            if (!userGet.IpAddressHistory.Contains(value: hashedIpAddress))
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
                _ = await _userRepository.UpdateUserAsync(updateDto: updateDto);
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
            if (_httpContextAccessor.HttpContext != null)
            {
                await _httpContextAccessor.HttpContext.SignOutAsync(scheme: CookieAuthenticationDefaults.AuthenticationScheme);
                _logManagerService.Log(severity: LogSeverity.Information, message: "UserLogin logged out");
            }
        }

        public string HashPassword(string password)
        {
            // Normalize the password to ensure consistent hashing
            password = password.Normalize(normalizationForm: System.Text.NormalizationForm.FormKD);
            byte[] bytes = SHA256.HashData(source: Encoding.UTF8.GetBytes(s: password));

            // Use lowercase hex format for consistency
            StringBuilder builder = new();
            for (int i = 0; i < bytes.Length; i++)
            {
                _ = builder.Append(value: bytes[i].ToString(format: "x2"));
            }

            // Return lowercase hex string
            return builder.ToString().ToLowerInvariant();
        }

        public bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(value: password) || string.IsNullOrEmpty(value: storedHash))
            {
                return false;
            }
            string hashedPassword = HashPassword(password: password);
            // Use case-insensitive comparison for hashes
            return string.Equals(a: hashedPassword, b: storedHash, comparisonType: StringComparison.OrdinalIgnoreCase);
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

                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(days: 7),
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

            if (_httpContextAccessor.HttpContext != null)
            {
                await _httpContextAccessor.HttpContext.SignInAsync(
scheme: CookieAuthenticationDefaults.AuthenticationScheme,
principal: new ClaimsPrincipal(claimsIdentity),
properties: authProperties);
            }
            else
            {
                _logManagerService.Log(severity: LogSeverity.Error, message: "HttpContext is null. Cannot sign in user.");
                // Handle the error appropriately, maybe throw an exception
                throw new InvalidOperationException("HttpContext is not available to sign in the user.");
            }
        }
    }
}
