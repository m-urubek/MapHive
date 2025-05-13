namespace MapHive.Services
{
    using MapHive.Models;
    using MapHive.Models.BusinessModels;

    public interface IAccountService
    {
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task LogoutAsync();
        bool VerifyPassword(string password, string storedHash);
        string HashPassword(string password);

        /// <summary>
        /// Changes the username for the given user.
        /// </summary>
        Task ChangeUsernameAsync(int userId, string newUsername);

        /// <summary>
        /// Changes the password for the given user.
        /// </summary>
        Task ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    }
}