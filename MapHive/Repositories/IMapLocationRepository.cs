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
    }
}