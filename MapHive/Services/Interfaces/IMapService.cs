namespace MapHive.Services
{
    using MapHive.Models.RepositoryModels;
    using MapHive.Models.ViewModels;

    public interface IMapService
    {
        Task<IEnumerable<MapLocationGet>> GetAllLocationsAsync();
        Task<IEnumerable<CategoryGet>> GetAllCategoriesAsync();
        Task<MapLocationGet?> GetLocationByIdAsync(int id);
        Task<MapLocationGet> GetLocationByIdOrThrowAsync(int id);
        Task<MapLocationGet> AddLocationAsync(MapLocationCreate mapLocationCreate);
        Task<MapLocationGet?> UpdateLocationAsync(int id, MapLocationUpdate updateDto);
        Task<bool> DeleteLocationAsync(int id);
        Task<MapLocationViewModel> GetLocationDetailsAsync(int id);
        Task<MapLocationGet?> GetLocationWithCategoryAsync(int id);
        Task<bool?> HasCurrentUserReviewedLocationAsync(int locationId);
        Task<AddLocationPageViewModel> GetAddLocationPageViewModelAsync();
        Task<EditLocationPageViewModel> GetEditLocationPageViewModelAsync(int id);
    }
}
