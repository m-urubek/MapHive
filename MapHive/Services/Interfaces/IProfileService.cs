namespace MapHive.Services
{
    using MapHive.Models.ViewModels;

    public interface IProfileService
    {
        Task<PrivateProfileViewModel> GetPrivateProfileAsync();
        Task<PublicProfileViewModel> GetPublicProfileAsync(string username);
        Task<PublicProfileViewModel> GetPublicProfileAsync(int accountId);
    }
}