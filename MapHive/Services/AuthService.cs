namespace MapHive.Services;

using System.Data;
using System.Security.Claims;
using MapHive.Models.BusinessModels;
using MapHive.Models.Data;
using MapHive.Models.Data.DbTableModels;
using MapHive.Models.Enums;
using MapHive.Models.Exceptions;
using MapHive.Models.PageModels;
using MapHive.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

public class AuthService(IAccountsRepository _userRepository,
    ILogManagerService _logManagerService,
    IHttpContextAccessor _httpContextAccessor,
    IIpBansRepository _ipBansRepository,
    IAccountBansRepository _accountBansRepository,
    IRequestContextService _requestContextService) : IAuthService
{

    public async Task<AuthResponse> RegisterAsync(RegisterPageModel pageModel)
    {
        // Check if username already exists
        if (await _userRepository.CheckUsernameExistsAsync(username: pageModel.Username ?? throw new NoNullAllowedException(nameof(pageModel.Username))))
        {
            throw new OrangeUserException("Username already exists");
        }

        // Check if IP is banned
        if (await _ipBansRepository.IsIpBannedAsync(hashedIpAddress: _requestContextService.HashedIpAddress))
        {
            // LogRaw the attempt but throw a user-friendly (less informative) error
            _ = _logManagerService.LogAsync(severity: LogSeverity.Warning, message: $"Registration attempt from banned IP: {_requestContextService.HashedIpAddress}");
            throw new OrangeUserException("Registration failed due to security reasons.");
        }

        int accountId = await _userRepository.CreateAccountAsync(
            username: pageModel.Username ?? throw new NoNullAllowedException(nameof(pageModel.Username)),
            passwordHash: HashPassword(password: pageModel.Password ?? throw new NoNullAllowedException(nameof(pageModel.Password))),
            registrationDate: DateTime.UtcNow,
            tier: AccountTier.Normal,
            ipAddressHistory: _requestContextService.HashedIpAddress
        );

        // Retrieve the created user
        AccountAtomic? accountGet = await _userRepository.GetAccountByIdAsync(id: accountId) ?? throw new InvalidOperationException("Unable to retrieve newly created user.");

        // Automatically log in the user after registration
        await SignInUserAsync(user: new UserLogin()
        {
            Id = accountGet.Id,
            Username = accountGet.Username,
            Tier = accountGet.Tier
        });

        // Log registration, including hashed IP after sign-in so AuthorId is available
        _ = _logManagerService.LogAsync(severity: LogSeverity.Information, message: $"New user registered: {accountId}",
            additionalData: $"Hashed IP: {_requestContextService.HashedIpAddress}");

        return new AuthResponse
        {
            Success = true,
            Message = "Registration successful",
            User = accountGet // Return the created user object
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginPageModel request)
    {
        // Get user by username
        AccountAtomic? accountGet = await _userRepository.GetAccountByUsernameAsync(username: request.Username);
        if (accountGet == null)
        {
            _ = _logManagerService.LogAsync(severity: LogSeverity.Warning, message: $"Failed login attempt for username: {request.Username} from IP: {_requestContextService.HashedIpAddress}");
            throw new OrangeUserException("Invalid username or password");
        }

        // Check password
        if (!VerifyPassword(password: request.Password, storedHash: accountGet.PasswordHash))
        {
            _ = _logManagerService.LogAsync(severity: LogSeverity.Warning, message: $"Failed login attempt for username: {request.Username} from IP: {_requestContextService.HashedIpAddress}");
            throw new OrangeUserException("Invalid username or password");
        }

        // Check if the user is banned
        AccountBanExtended? activeBan = await _accountBansRepository.GetActiveAccountBanByAccountIdAsync(accountId: accountGet.Id);
        if (activeBan != null)
        {
            _ = _logManagerService.LogAsync(severity: LogSeverity.Warning, message: $"Banned user login attempt: {request.Username} (Ban ID: {activeBan.Id})");
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
        if (accountGet.Tier != AccountTier.Admin && await _ipBansRepository.IsIpBannedAsync(hashedIpAddress: _requestContextService.HashedIpAddress))
        {
            _ = _logManagerService.LogAsync(severity: LogSeverity.Warning, message: $"Login attempt from banned IP: {_requestContextService.HashedIpAddress} for user: {request.Username}");
            throw new OrangeUserException("Login failed due to security reasons.");
        }

        // Sign in the user before logging so HttpContext.User includes the ID
        await SignInUserAsync(user: new UserLogin()
        {
            Id = accountGet.Id,
            Username = accountGet.Username,
            Tier = accountGet.Tier
        });

        // Log successful login after sign-in
        _ = _logManagerService.LogAsync(severity: LogSeverity.Information, message: $"UserLogin logged in: {request.Username}", additionalData: $"IP: {_requestContextService.HashedIpAddress}");

        // Update IP history if necessary (consider rate limiting or specific logic)
        if (!accountGet.IpAddressHistory.Contains(value: _requestContextService.HashedIpAddress))
        {
            await _userRepository.UpdateAccountOrThrowAsync(
                id: accountGet.Id,
                username: DynamicValue<string>.Unassigned(),
                passwordHash: DynamicValue<string>.Unassigned(),
                tier: DynamicValue<AccountTier>.Unassigned(),
                ipAddressHistory: DynamicValue<string>.Set(_requestContextService.HashedIpAddress)
            );
        }

        return new AuthResponse
        {
            Success = true,
            Message = "Login successful",
            User = accountGet
        };
    }

    public async Task LogoutAsync()
    {
        if (_httpContextAccessor.HttpContext != null)
        {
            await _httpContextAccessor.HttpContext.SignOutAsync(scheme: CookieAuthenticationDefaults.AuthenticationScheme);
            _ = _logManagerService.LogAsync(severity: LogSeverity.Information, message: "User logged out");
        }
    }

    public string HashPassword(string password)
    {
        return MapHive.Utilities.HashingUtility.HashPassword(password: password);
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
            new(type: ClaimTypes.NameIdentifier, value: user.Id.ToString()),
            new(type: ClaimTypes.Name, value: user.Username),
            new(type: ClaimTypes.Role, value: user.Tier.ToString()), // Add tier number as role
            new(type: ClaimTypes.Role, value: user.Tier.ToString()) // Add tier name as role
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
            ClaimsPrincipal principal = new(claimsIdentity);
            await _httpContextAccessor.HttpContext.SignInAsync(
                scheme: CookieAuthenticationDefaults.AuthenticationScheme,
                principal: principal,
                properties: authProperties);
            // Update the HttpContext.User for the current request
            _httpContextAccessor.HttpContext.User = principal;
        }
        else
        {
            _ = _logManagerService.LogAsync(severity: LogSeverity.Error, message: "HttpContext is null. Cannot sign in user.");
            // Handle the error appropriately, maybe throw an exception
            throw new InvalidOperationException("HttpContext is not available to sign in the user.");
        }
    }
}
