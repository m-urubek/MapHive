namespace MapHive.Services
{
    using MapHive.Models;
    using MapHive.Models.BusinessModels;
    using MapHive.Models.RepositoryModels;
    using MapHive.Models.ViewModels;

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
        Task ChangeUsernameAsync(int accountId, string newUsername);

        /// <summary>
        /// Changes the password for the given user.
        /// </summary>
        Task ChangePasswordAsync(int accountId, string currentPassword, string newPassword);

        Task<BanViewModel?> GetActiveBanViewModelAsync(int accountId);

    }
}