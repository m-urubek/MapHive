namespace MapHive.Services;

using MapHive.Models.BusinessModels;
using MapHive.Models.Data.DbTableModels;
using MapHive.Models.PageModels;
public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterPageModel pageModel);
    Task<AuthResponse> LoginAsync(LoginPageModel pageModel);
    Task<AuthResponse> LoginOrRegisterWithDiscordAsync(string discordId, string discordUsername);
    Task LogoutAsync();
    string HashPassword(string password);
    bool VerifyPassword(string password, string storedHash);
    Task SignInUserDirectAsync(AccountAtomic account);
}