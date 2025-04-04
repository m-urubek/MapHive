using MapHive.Models;

namespace MapHive.Services
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request, string ipAddress);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        bool IsBlacklisted(string ipAddress);
        string HashPassword(string password);
        bool VerifyPassword(string password, string storedHash);
    }
}