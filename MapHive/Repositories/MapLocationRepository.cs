using MapHive;
using MapHive.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;

namespace MapHive.Repositories
{
    public class MapLocationRepository : IMapLocationRepository
    {
        public async Task<IEnumerable<MapLocation>> GetAllLocationsAsync()
        {
            return await Task.Run(() =>
            {
                var locations = new List<MapLocation>();
                DataTable dataTable = MainClient.SqlClient.Select("SELECT * FROM MapLocations");
                
                foreach (DataRow row in dataTable.Rows)
                {
                    locations.Add(MapDataRowToMapLocation(row));
                }
                
                return locations;
            });
        }

        public async Task<MapLocation> GetLocationByIdAsync(int id)
        {
            return await Task.Run(() =>
            {
                var parameters = new SQLiteParameter[]
                {
                    new SQLiteParameter("@Id", id)
                };
                
                DataTable dataTable = MainClient.SqlClient.Select(
                    "SELECT * FROM MapLocations WHERE Id = @Id", 
                    parameters);
                
                if (dataTable.Rows.Count == 0)
                    return null;
                
                return MapDataRowToMapLocation(dataTable.Rows[0]);
            });
        }

        public async Task<MapLocation> AddLocationAsync(MapLocation location)
        {
            return await Task.Run(() =>
            {
                location.CreatedAt = DateTime.UtcNow;
                location.UpdatedAt = DateTime.UtcNow;
                
                var parameters = new SQLiteParameter[]
                {
                    new SQLiteParameter("@Name", location.Name),
                    new SQLiteParameter("@Description", location.Description),
                    new SQLiteParameter("@Latitude", location.Latitude),
                    new SQLiteParameter("@Longitude", location.Longitude),
                    new SQLiteParameter("@Address", location.Address ?? (object)DBNull.Value),
                    new SQLiteParameter("@Website", location.Website ?? (object)DBNull.Value),
                    new SQLiteParameter("@PhoneNumber", location.PhoneNumber ?? (object)DBNull.Value),
                    new SQLiteParameter("@CreatedAt", location.CreatedAt),
                    new SQLiteParameter("@UpdatedAt", location.UpdatedAt)
                };
                
                int id = MainClient.SqlClient.Insert(
                    @"INSERT INTO MapLocations (Name, Description, Latitude, Longitude, 
                      Address, Website, PhoneNumber, CreatedAt, UpdatedAt) 
                      VALUES (@Name, @Description, @Latitude, @Longitude, 
                      @Address, @Website, @PhoneNumber, @CreatedAt, @UpdatedAt)",
                    parameters);
                
                location.Id = id;
                return location;
            });
        }

        public async Task<MapLocation> UpdateLocationAsync(MapLocation location)
        {
            return await Task.Run(async () =>
            {
                var existingLocation = await GetLocationByIdAsync(location.Id);
                
                if (existingLocation == null)
                    return null;

                location.CreatedAt = existingLocation.CreatedAt;
                location.UpdatedAt = DateTime.UtcNow;
                
                var parameters = new SQLiteParameter[]
                {
                    new SQLiteParameter("@Id", location.Id),
                    new SQLiteParameter("@Name", location.Name),
                    new SQLiteParameter("@Description", location.Description),
                    new SQLiteParameter("@Latitude", location.Latitude),
                    new SQLiteParameter("@Longitude", location.Longitude),
                    new SQLiteParameter("@Address", location.Address ?? (object)DBNull.Value),
                    new SQLiteParameter("@Website", location.Website ?? (object)DBNull.Value),
                    new SQLiteParameter("@PhoneNumber", location.PhoneNumber ?? (object)DBNull.Value),
                    new SQLiteParameter("@CreatedAt", location.CreatedAt),
                    new SQLiteParameter("@UpdatedAt", location.UpdatedAt)
                };
                
                MainClient.SqlClient.Update(
                    @"UPDATE MapLocations 
                      SET Name = @Name, Description = @Description, 
                      Latitude = @Latitude, Longitude = @Longitude, 
                      Address = @Address, Website = @Website, 
                      PhoneNumber = @PhoneNumber, CreatedAt = @CreatedAt, 
                      UpdatedAt = @UpdatedAt 
                      WHERE Id = @Id",
                    parameters);
                
                return location;
            });
        }

        public async Task<bool> DeleteLocationAsync(int id)
        {
            return await Task.Run(async () =>
            {
                var location = await GetLocationByIdAsync(id);
                
                if (location == null)
                    return false;

                var parameters = new SQLiteParameter[]
                {
                    new SQLiteParameter("@Id", id)
                };
                
                int rowsAffected = MainClient.SqlClient.Delete(
                    "DELETE FROM MapLocations WHERE Id = @Id",
                    parameters);
                
                return rowsAffected > 0;
            });
        }

        private MapLocation MapDataRowToMapLocation(DataRow row)
        {
            return new MapLocation
            {
                Id = Convert.ToInt32(row["Id"]),
                Name = row["Name"].ToString(),
                Description = row["Description"].ToString(),
                Latitude = Convert.ToDouble(row["Latitude"]),
                Longitude = Convert.ToDouble(row["Longitude"]),
                Address = row["Address"] != DBNull.Value ? row["Address"].ToString() : null,
                Website = row["Website"] != DBNull.Value ? row["Website"].ToString() : null,
                PhoneNumber = row["PhoneNumber"] != DBNull.Value ? row["PhoneNumber"].ToString() : null,
                CreatedAt = Convert.ToDateTime(row["CreatedAt"]),
                UpdatedAt = Convert.ToDateTime(row["UpdatedAt"])
            };
        }
    }
} 