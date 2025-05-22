namespace MapHive.Services;

using MapHive.Models.PageModels;

public interface IProfileService
{
    Task<PrivateProfilePageModel> GetPrivateProfilePageModelAsync();
    Task<PublicProfilePageModel> GetPublicProfileAsync(string username);
    Task<PublicProfilePageModel> GetPublicProfileAsync(int accountId);
}