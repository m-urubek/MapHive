using MapHive.Models.ViewModels;

namespace MapHive.Services
{
    public interface IProfileService
    {
        Task<PrivateProfileViewModel?> GetPrivateProfileAsync(int userId);
        Task<PublicProfileViewModel?> GetPublicProfileAsync(string username);
    }
}