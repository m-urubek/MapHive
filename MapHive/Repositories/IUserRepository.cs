using MapHive.Models;

namespace MapHive.Repositories
{
    public interface IUserRepository
    {
        int CreateUser(User user);
        User? GetUserById(int id);
        User? GetUserByUsername(string username);
        bool CheckUsernameExists(string username);
        bool IsBlacklisted(string ipAddress);
        int AddToBlacklist(BlacklistedAddress blacklistedAddress);
        void UpdateUser(User user);
        Task<string> GetUsernameByIdAsync(int userId);
    }
}