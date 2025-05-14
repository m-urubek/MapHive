namespace MapHive.Services
{
    using MapHive.Models;
    using MapHive.Models.BusinessModels;

    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task LogoutAsync();
        string HashPassword(string password);
        bool VerifyPassword(string password, string storedHash);
    }
}