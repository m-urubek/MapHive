using MapHive.Models.RepositoryModels;

namespace MapHive.Repositories
{
    public interface IMapLocationRepository
    {
        Task<IEnumerable<MapLocationGet>> GetAllLocationsAsync();
        Task<MapLocationGet?> GetLocationByIdAsync(int id);
        Task<MapLocationGet> AddLocationAsync(MapLocationCreate location);
        Task<MapLocationGet?> UpdateLocationAsync(MapLocationUpdate location);
        Task<bool> DeleteLocationAsync(int id);
        Task<IEnumerable<MapLocationGet>> GetLocationsByUserIdAsync(int userId);
        Task<MapLocationGet?> GetLocationWithCategoryAsync(int id);

        // Category methods
        Task<IEnumerable<CategoryGet>> GetAllCategoriesAsync();
        Task<CategoryGet?> GetCategoryByIdAsync(int id);
        Task<CategoryGet> AddCategoryAsync(CategoryCreate category);
        Task<CategoryGet?> UpdateCategoryAsync(CategoryUpdate category);
        Task<bool> DeleteCategoryAsync(int id);
    }
}