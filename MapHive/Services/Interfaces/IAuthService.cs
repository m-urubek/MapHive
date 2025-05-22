namespace MapHive.Services;

using MapHive.Models.BusinessModels;
using MapHive.Models.PageModels;
public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterPageModel pageModel);
    Task<AuthResponse> LoginAsync(LoginPageModel pageModel);
    Task LogoutAsync();
    string HashPassword(string password);
    bool VerifyPassword(string password, string storedHash);
}