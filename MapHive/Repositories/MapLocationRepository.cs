using MapHive.Data;
using MapHive.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MapHive.Repositories
{
    public class MapLocationRepository : IMapLocationRepository
    {
        private readonly ApplicationDbContext _context;

        public MapLocationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<MapLocation>> GetAllLocationsAsync()
        {
            return await _context.MapLocations.ToListAsync();
        }

        public async Task<MapLocation> GetLocationByIdAsync(int id)
        {
            return await _context.MapLocations.FindAsync(id);
        }

        public async Task<MapLocation> AddLocationAsync(MapLocation location)
        {
            location.CreatedAt = DateTime.UtcNow;
            location.UpdatedAt = DateTime.UtcNow;
            
            _context.MapLocations.Add(location);
            await _context.SaveChangesAsync();
            return location;
        }

        public async Task<MapLocation> UpdateLocationAsync(MapLocation location)
        {
            var existingLocation = await _context.MapLocations.FindAsync(location.Id);
            
            if (existingLocation == null)
                return null;

            location.CreatedAt = existingLocation.CreatedAt;
            location.UpdatedAt = DateTime.UtcNow;
            
            _context.Entry(existingLocation).CurrentValues.SetValues(location);
            await _context.SaveChangesAsync();
            
            return existingLocation;
        }

        public async Task<bool> DeleteLocationAsync(int id)
        {
            var location = await _context.MapLocations.FindAsync(id);
            
            if (location == null)
                return false;

            _context.MapLocations.Remove(location);
            await _context.SaveChangesAsync();
            
            return true;
        }
    }
} 