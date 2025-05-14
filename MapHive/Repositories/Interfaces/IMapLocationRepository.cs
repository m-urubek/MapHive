namespace MapHive.Repositories
{
    using MapHive.Models.RepositoryModels;

    public interface IMapLocationRepository
    {
        Task<IEnumerable<MapLocationGet>> GetAllLocationsAsync();
        Task<MapLocationGet?> GetLocationByIdAsync(int id);
        Task<MapLocationGet> GetLocationByIdOrThrowAsync(int id);
        Task<MapLocationGet> AddLocationAsync(MapLocationCreate location);
        Task<MapLocationGet?> UpdateLocationAsync(MapLocationUpdate location);
        Task<bool> DeleteLocationAsync(int id);
        Task<IEnumerable<MapLocationGet>> GetLocationsByAccountIdAsync(int accountId);
        Task<MapLocationGet?> GetLocationWithCategoryAsync(int id);
        Task<MapLocationGet> GetLocationWithCategoryOrThrowAsync(int id);

        // CategoryName methods
        Task<IEnumerable<CategoryGet>> GetAllCategoriesAsync();
        Task<CategoryGet?> GetCategoryByIdAsync(int id);
        Task<CategoryGet> GetCategoryByIdOrThrowAsync(int id);
        Task<CategoryGet> AddCategoryAsync(CategoryCreate category);
        Task<CategoryGet?> UpdateCategoryAsync(CategoryUpdate category);
        Task<bool> DeleteCategoryAsync(int id);
    }
}
