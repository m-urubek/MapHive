using MapHive.Models;

namespace MapHive.Repositories
{
    public interface IMapLocationRepository
    {
        Task<IEnumerable<MapLocation>> GetAllLocationsAsync();
        Task<MapLocation> GetLocationByIdAsync(int id);
        Task<MapLocation> AddLocationAsync(MapLocation location);
        Task<MapLocation> UpdateLocationAsync(MapLocation location);
        Task<bool> DeleteLocationAsync(int id);
        Task<IEnumerable<MapLocation>> GetLocationsByUserIdAsync(int userId);

        // Category methods
        Task<IEnumerable<Category>> GetAllCategoriesAsync();
        Task<Category?> GetCategoryByIdAsync(int id);
        Task<Category> AddCategoryAsync(Category category);
        Task<Category> UpdateCategoryAsync(Category category);
        Task<bool> DeleteCategoryAsync(int id);
    }
}