namespace MapHive.Services
{
    using MapHive.Models.ViewModels;

    public interface IProfileService
    {
        Task<PrivateProfileViewModel> GetPrivateProfileAsync(int userId);
        Task<PublicProfileViewModel> GetPublicProfileAsync(string username);
    }
}