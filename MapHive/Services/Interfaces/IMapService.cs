namespace MapHive.Services
{
    using MapHive.Models.RepositoryModels;
    using MapHive.Models.ViewModels;

    public interface IMapService
    {
        Task<IEnumerable<MapLocationGet>> GetAllLocationsAsync();
        Task<IEnumerable<CategoryGet>> GetAllCategoriesAsync();
        Task<MapLocationGet?> GetLocationByIdAsync(int id);
        Task<MapLocationGet> AddLocationAsync(MapLocationCreate mapLocationCreate);
        Task<MapLocationGet?> UpdateLocationAsync(int id, MapLocationUpdate updateDto, int currentUserId, bool isAdmin);
        Task<bool> DeleteLocationAsync(int id, int currentUserId, bool isAdmin);
        Task<MapLocationViewModel> GetLocationDetailsAsync(int id, int? currentUserId);
        Task<MapLocationGet?> GetLocationWithCategoryAsync(int id);
        Task<bool> HasUserReviewedLocationAsync(int userId, int locationId);
        Task<AddLocationPageViewModel> GetAddLocationPageViewModelAsync(int currentUserId);
        Task<EditLocationPageViewModel> GetEditLocationPageViewModelAsync(int id, int currentUserId, bool isAdmin);
        Task<MapLocationGet> GetLocationForDeleteAsync(int id, int currentUserId, bool isAdmin);
    }
}
