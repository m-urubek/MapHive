namespace MapHive.Repositories;

using MapHive.Models.Data;
using MapHive.Models.Data.DbTableModels;

public interface IMapLocationRepository
{
    Task<IEnumerable<LocationExtended>> GetAllLocationsAsync();
    Task<LocationExtended?> GetLocationByIdAsync(int id);
    Task<LocationExtended> GetLocationByIdOrThrowAsync(int id);
    Task<int> CreateLocationAsync(
        string name,
        string? description,
        double latitude,
        double longitude,
        string? address,
        string? website,
        string? phoneNumber,
        bool isAnonymous,
        int categoryId,
        int ownerId
    );
    Task UpdateLocationOrThrowAsync(
        int id,
        DynamicValue<string> name,
        DynamicValue<string?> description,
        DynamicValue<double> latitude,
        DynamicValue<double> longitude,
        DynamicValue<string?> address,
        DynamicValue<string?> website,
        DynamicValue<string?> phoneNumber,
        DynamicValue<bool> isAnonymous,
        DynamicValue<int> categoryId
    );
    Task<bool> DeleteLocationAsync(int id);
    Task<IEnumerable<LocationExtended>> GetLocationsByOwnerIdAsync(int accountId);
    Task<IEnumerable<CategoryAtomic>> GetAllCategoriesAsync();
}
