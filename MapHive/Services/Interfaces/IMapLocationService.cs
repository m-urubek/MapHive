namespace MapHive.Services;

using MapHive.Models.Data.DbTableModels;
using MapHive.Models.PageModels;

public interface IMapLocationService
{
    Task<LocationExtended> GetLocationByIdOrThrowAsync(int id);
    Task UpdateLocationOrThrowAsync(int id, LocationUpdatePageModel locationUpdatePageModel);
    Task<bool> DeleteLocationAsync(int id);
    Task<LocationDisplayPageModel> GetLocationDetailsAsync(int id);
    Task<LocationUpdatePageModel> GetAddLocationPagePageModelAsync();
    Task<LocationUpdatePageModel> GetLocationUpdatePageModelAsync(int id);
    void EnsureUserCanEditLocation(int locationAuthorId);
}
