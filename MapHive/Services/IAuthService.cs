using MapHive.Models;

namespace MapHive.Services
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request, string ipAddress, string macAddress);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        bool IsBlacklisted(string ipAddress, string macAddress);
        string HashPassword(string password);
        bool VerifyPassword(string password, string storedHash);
    }
}