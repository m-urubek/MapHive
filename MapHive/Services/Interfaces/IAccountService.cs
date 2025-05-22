namespace MapHive.Services;

using MapHive.Models.BusinessModels;
using MapHive.Models.PageModels;

public interface IAccountService
{
    Task<AuthResponse> LoginAsync(LoginPageModel request);
    Task<AuthResponse> RegisterAsync(RegisterPageModel request);
    Task LogoutAsync();
    bool VerifyPassword(string password, string storedHash);
    string HashPassword(string password);

    /// <summary>
    /// Changes the username for the given user.
    /// </summary>
    Task ChangeUsernameAsync(string newUsername);

    /// <summary>
    /// Changes the password for the given user.
    /// </summary>
    Task ChangePasswordAsync(string currentPassword, string newPassword);

    Task<BanOnProfilePageModel?> GetActiveBanPageModelAsync(int accountId);
    Task UnbanUserAsync(int accountId);
    Task<int> BanAsync(int accountId, BanUserUpdatePageModel banPageModel);
    Task<BanUserUpdatePageModel> GetBanUserPagePageModelAsync(int accountId);
    Task SetDarkModePreferenceAsync(bool enabled);
    Task<bool> GetDarkModePreferenceAsync();
}