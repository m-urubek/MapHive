namespace MapHive.Services;

using System.Data;
using MapHive.Models.BusinessModels;
using MapHive.Models.Data;
using MapHive.Models.Data.DbTableModels;
using MapHive.Models.Enums;
using MapHive.Models.Exceptions;
using MapHive.Models.PageModels;
using MapHive.Repositories;

public class AccountService(
    IAuthService _authService,
    ILogManagerService _logManagerService,
    IAccountsRepository _accountsRepository,
    IRequestContextService _requestContextService,
    IAccountBansRepository _accountBansRepository,
    IIpBansRepository _ipBansRepository,
    IUserContextService _userContextService) : IAccountService
{

    public async Task<AuthResponse> LoginAsync(LoginPageModel pageModel)
    {
        ArgumentNullException.ThrowIfNull(pageModel);

        if (string.IsNullOrEmpty(value: _requestContextService.HashedIpAddress))
            throw new PublicWarningException("Unable to retrieve your IP address. Login cannot proceed.");

        AuthResponse response = await _authService.LoginAsync(pageModel: pageModel);
        _ = _logManagerService.LogAsync(severity: LogSeverity.Information, message: $"User {pageModel.Username} successfully logged in.");
        return response;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterPageModel pageModel)
    {
        ArgumentNullException.ThrowIfNull(pageModel);

        if (string.IsNullOrEmpty(value: _requestContextService.HashedIpAddress))
            throw new PublicWarningException("Unable to retrieve your IP address. Login cannot proceed.");

        AuthResponse response = await _authService.RegisterAsync(pageModel: pageModel);
        _ = _logManagerService.LogAsync(severity: LogSeverity.Information, message: $"User {pageModel.Username} successfully registered and logged in.");
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

    public async Task ChangeUsernameAsync(string newUsername)
    {
        // Retrieve user
        AccountAtomic accountGet = await _accountsRepository.GetAccountByIdOrThrowAsync(id: _userContextService.AccountIdOrThrow);

        // Check username availability
        if (await _accountsRepository.CheckUsernameExistsAsync(username: newUsername)
            && !accountGet.Username.Equals(value: newUsername, comparisonType: StringComparison.OrdinalIgnoreCase))
        {
            throw new OrangeUserException("Username already exists");
        }

        // Update username
        accountGet.Username = newUsername;
        await _accountsRepository.UpdateAccountOrThrowAsync(
            id: accountGet.Id,
            username: DynamicValue<string>.Set(newUsername),
            passwordHash: DynamicValue<string>.Unassigned(),
            tier: DynamicValue<AccountTier>.Unassigned(),
            ipAddressHistory: DynamicValue<string>.Unassigned());
    }

    public async Task ChangePasswordAsync(string currentPassword, string newPassword)
    {
        // Retrieve user
        AccountAtomic accountGet = await _accountsRepository.GetAccountByIdOrThrowAsync(id: _userContextService.AccountIdOrThrow);

        // Verify current password
        if (!VerifyPassword(password: currentPassword, storedHash: accountGet.PasswordHash))
        {
            throw new OrangeUserException("Current password is incorrect");
        }

        // Update password hash
        await _accountsRepository.UpdateAccountOrThrowAsync(
            id: accountGet.Id,
            username: DynamicValue<string>.Unassigned(),
            passwordHash: DynamicValue<string>.Set(HashPassword(password: newPassword)),
            tier: DynamicValue<AccountTier>.Unassigned(),
            ipAddressHistory: DynamicValue<string>.Unassigned()
        );
    }

    public async Task<BanOnProfilePageModel?> GetActiveBanPageModelAsync(int accountId)
    {
        AccountBanExtended? accountBanGet = await _accountBansRepository.GetActiveAccountBanByAccountIdAsync(accountId: accountId);
        if (accountBanGet != null)
        {
            return new()
            {
                BanType = BanType.Account,
                Reason = accountBanGet.Reason,
                BannedByUsername = accountBanGet.BannedByUsername,
                BannedById = accountBanGet.BannedByAccountId,
                CreatedDateTime = accountBanGet.CreatedDateTime.ToString("g"),
                ExpiresAt = accountBanGet.ExpiresAt
            };
        }
        else
        {
            IpBanExtended? ipBanGet = await _ipBansRepository.GetActiveIpBanByIpAddressAsync(hashedIpAddress: _requestContextService.HashedIpAddress);
            if (ipBanGet != null)
            {
                return new()
                {
                    BanType = BanType.IpAddress,
                    Reason = ipBanGet.Reason,
                    BannedByUsername = ipBanGet.BannedByUsername,
                    BannedById = ipBanGet.BannedByAccountId,
                    CreatedDateTime = ipBanGet.CreatedDateTime.ToString("g"),
                    ExpiresAt = ipBanGet.ExpiresAt
                };
            }
        }
        return null;
    }

    /// <summary>
    /// Removes the active ban for the specified account ID or, if none, the active IP ban for the current request.
    /// </summary>
    public async Task UnbanUserAsync(int accountId)
    {
        // Try to remove an active account ban first
        AccountBanExtended? activeAccountBan = await _accountBansRepository.GetActiveAccountBanByAccountIdAsync(accountId: accountId);
        if (activeAccountBan != null)
        {
            await _accountBansRepository.RemoveAccountBanAsync(banId: activeAccountBan.Id);
            return;
        }
        // If no account ban, try to remove an active IP ban for this request
        IpBanExtended? activeIpBan = await _ipBansRepository.GetActiveIpBanByIpAddressAsync(hashedIpAddress: _requestContextService.HashedIpAddress) ?? throw new PublicWarningException("No ban found.");

        await _ipBansRepository.RemoveIpBanAsync(banId: activeIpBan.Id);
    }

    /// <summary>
    /// Gets data for rendering the Ban User page from Profiles.
    /// </summary>
    public async Task<BanUserUpdatePageModel> GetBanUserPagePageModelAsync(int userBeingBannedId)
    {
        AccountAtomic userBeingBannedGet = await _accountsRepository.GetAccountByIdOrThrowAsync(id: userBeingBannedId);

        return new BanUserUpdatePageModel
        {
            BanType = BanType.Account,
            Reason = null,
            IsPermanent = false,
            BanDurationDays = null,
            Username = userBeingBannedGet.Username,
            AccountTier = userBeingBannedGet.Tier
        };
    }

    public async Task<int> BanAsync(int accountId, BanUserUpdatePageModel banPageModel)
    {
        AccountAtomic accountGet = await _accountsRepository.GetAccountByIdOrThrowAsync(id: accountId);
        return accountGet.Tier == AccountTier.Admin
            ? throw new PublicErrorException("You cannot ban an admin account.")
            : banPageModel.BanType == BanType.Account
            ? await _accountBansRepository.CreateAccountBanAsync(
                accountId: accountId,
                banCreatedDateTime: DateTime.UtcNow,
                expiresAt: banPageModel.IsPermanent ? null : DateTime.UtcNow.AddDays(banPageModel.BanDurationDays ?? throw new NoNullAllowedException(nameof(banPageModel.BanDurationDays))),
                reason: banPageModel.Reason,
                bannedByAccountId: _userContextService.AccountIdOrThrow
            )
            : await _ipBansRepository.CreateIpBanAsync(
                hashedIpAddress: accountGet.IpAddressHistory.Split(separator: Environment.NewLine, options: StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? throw new Exception("Unable to read last login IP of an account from database"),
                banCreatedDateTime: DateTime.UtcNow,
                expiresAt: banPageModel.IsPermanent ? null : DateTime.UtcNow.AddDays(banPageModel.BanDurationDays ?? throw new NoNullAllowedException(nameof(banPageModel.BanDurationDays))),
                reason: banPageModel.Reason,
                bannedByAccountId: _userContextService.AccountIdOrThrow
            );
    }

    public async Task SetDarkModePreferenceAsync(bool enabled)
    {
        int userId = _userContextService.AccountIdOrThrow;
        await _accountsRepository.UpdateDarkModePreferenceAsync(userId, enabled);
        // Update claim for dark mode in authentication
        await _userContextService.SetClaim(claimKey: "DarkModeEnabled", claimValue: enabled.ToString());
    }

    public async Task<bool> GetDarkModePreferenceAsync()
    {
        int userId = _userContextService.AccountIdOrThrow;
        AccountAtomic account = await _accountsRepository.GetAccountByIdOrThrowAsync(userId);
        return account.DarkModeEnabled;
    }
}
