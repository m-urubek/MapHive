using MapHive.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

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