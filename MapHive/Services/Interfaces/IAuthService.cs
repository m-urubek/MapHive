using MapHive.Models;
using MapHive.Models.BusinessModels;

namespace MapHive.Services
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request, string ipAddress);
        Task<AuthResponse> LoginAsync(LoginRequest request, string ipAddress);
        Task LogoutAsync();
        string HashPassword(string password);
        bool VerifyPassword(string password, string storedHash);
    }
}