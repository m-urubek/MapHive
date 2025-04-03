using MapHive.Models;
using MapHive.Singletons;
using System.Data;
using System.Data.SQLite;

namespace MapHive.Repositories
{
    public class MapLocationRepository : IMapLocationRepository
    {
        public async Task<IEnumerable<MapLocation>> GetAllLocationsAsync()
        {
            return await Task.Run(() =>
            {
                List<MapLocation> locations = new();
                DataTable dataTable = MainClient.SqlClient.Select("SELECT * FROM MapLocations");

                foreach (DataRow row in dataTable.Rows)
                {
                    locations.Add(this.MapDataRowToMapLocation(row));
                }

                return locations;
            });
        }

        public async Task<MapLocation> GetLocationByIdAsync(int id)
        {
            return await Task.Run(() =>
            {
                SQLiteParameter[] parameters = new SQLiteParameter[]
                {
                    new("@Id", id)
                };

                DataTable dataTable = MainClient.SqlClient.Select(
                    "SELECT * FROM MapLocations WHERE Id_MapLocation = @Id",
                    parameters);

                return dataTable.Rows.Count == 0 ? null : this.MapDataRowToMapLocation(dataTable.Rows[0]);
            });
        }

        public async Task<MapLocation> AddLocationAsync(MapLocation location)
        {
            return await Task.Run(() =>
            {
                location.CreatedAt = DateTime.UtcNow;
                location.UpdatedAt = DateTime.UtcNow;

                SQLiteParameter[] parameters = new SQLiteParameter[]
                {
                    new("@Name", location.Name),
                    new("@Description", location.Description),
                    new("@Latitude", location.Latitude),
                    new("@Longitude", location.Longitude),
                    new("@Address", location.Address ?? (object)DBNull.Value),
                    new("@Website", location.Website ?? (object)DBNull.Value),
                    new("@PhoneNumber", location.PhoneNumber ?? (object)DBNull.Value),
                    new("@CreatedAt", location.CreatedAt),
                    new("@UpdatedAt", location.UpdatedAt),
                    new("@UserId", location.UserId),
                    new("@IsAnonymous", location.IsAnonymous ? 1 : 0)
                };

                int id = MainClient.SqlClient.Insert(
                    @"INSERT INTO MapLocations (Name, Description, Latitude, Longitude, 
                      Address, Website, PhoneNumber, CreatedAt, UpdatedAt, UserId, IsAnonymous) 
                      VALUES (@Name, @Description, @Latitude, @Longitude, 
                      @Address, @Website, @PhoneNumber, @CreatedAt, @UpdatedAt, @UserId, @IsAnonymous)",
                    parameters);

                location.Id = id;
                return location;
            });
        }

        public async Task<MapLocation> UpdateLocationAsync(MapLocation location)
        {
            return await Task.Run(async () =>
            {
                MapLocation existingLocation = await this.GetLocationByIdAsync(location.Id);

                if (existingLocation == null)
                {
                    return null;
                }

                location.CreatedAt = existingLocation.CreatedAt;
                location.UpdatedAt = DateTime.UtcNow;
                location.UserId = existingLocation.UserId; // Preserve the original creator

                SQLiteParameter[] parameters = new SQLiteParameter[]
                {
                    new("@Id", location.Id),
                    new("@Name", location.Name),
                    new("@Description", location.Description),
                    new("@Latitude", location.Latitude),
                    new("@Longitude", location.Longitude),
                    new("@Address", location.Address ?? (object)DBNull.Value),
                    new("@Website", location.Website ?? (object)DBNull.Value),
                    new("@PhoneNumber", location.PhoneNumber ?? (object)DBNull.Value),
                    new("@CreatedAt", location.CreatedAt),
                    new("@UpdatedAt", location.UpdatedAt),
                    new("@UserId", location.UserId),
                    new("@IsAnonymous", location.IsAnonymous ? 1 : 0)
                };

                _ = MainClient.SqlClient.Update(
                    @"UPDATE MapLocations 
                      SET Name = @Name, Description = @Description, 
                      Latitude = @Latitude, Longitude = @Longitude, 
                      Address = @Address, Website = @Website, 
                      PhoneNumber = @PhoneNumber, CreatedAt = @CreatedAt, 
                      UpdatedAt = @UpdatedAt, UserId = @UserId, IsAnonymous = @IsAnonymous 
                      WHERE Id_MapLocation = @Id",
                    parameters);

                return location;
            });
        }

        public async Task<bool> DeleteLocationAsync(int id)
        {
            return await Task.Run(async () =>
            {
                MapLocation location = await this.GetLocationByIdAsync(id);

                if (location == null)
                {
                    return false;
                }

                SQLiteParameter[] parameters = new SQLiteParameter[]
                {
                    new("@Id", id)
                };

                int rowsAffected = MainClient.SqlClient.Delete(
                    "DELETE FROM MapLocations WHERE Id_MapLocation = @Id",
                    parameters);

                return rowsAffected > 0;
            });
        }

        public async Task<IEnumerable<MapLocation>> GetLocationsByUserIdAsync(int userId)
        {
            return await Task.Run(() =>
            {
                List<MapLocation> locations = new();
                SQLiteParameter[] parameters = new SQLiteParameter[]
                {
                    new("@UserId", userId)
                };

                DataTable dataTable = MainClient.SqlClient.Select(
                    "SELECT * FROM MapLocations WHERE UserId = @UserId",
                    parameters);

                foreach (DataRow row in dataTable.Rows)
                {
                    locations.Add(this.MapDataRowToMapLocation(row));
                }

                return locations;
            });
        }

        private MapLocation MapDataRowToMapLocation(DataRow row)
        {
            return new MapLocation
            {
                Id = Convert.ToInt32(row["Id_MapLocation"]),
                Name = row["Name"].ToString(),
                Description = row["Description"].ToString(),
                Latitude = Convert.ToDouble(row["Latitude"]),
                Longitude = Convert.ToDouble(row["Longitude"]),
                Address = row["Address"] != DBNull.Value ? row["Address"].ToString() : null,
                Website = row["Website"] != DBNull.Value ? row["Website"].ToString() : null,
                PhoneNumber = row["PhoneNumber"] != DBNull.Value ? row["PhoneNumber"].ToString() : null,
                CreatedAt = Convert.ToDateTime(row["CreatedAt"]),
                UpdatedAt = Convert.ToDateTime(row["UpdatedAt"]),
                UserId = row.Table.Columns.Contains("UserId") ? Convert.ToInt32(row["UserId"]) : 0,
                IsAnonymous = row.Table.Columns.Contains("IsAnonymous") && Convert.ToInt32(row["IsAnonymous"]) == 1
            };
        }

        #region Category Methods

        public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
        {
            return await Task.Run(() =>
            {
                List<Category> categories = new();
                DataTable dataTable = MainClient.SqlClient.Select("SELECT * FROM Categories");

                foreach (DataRow row in dataTable.Rows)
                {
                    categories.Add(this.MapDataRowToCategory(row));
                }

                return categories;
            });
        }

        public async Task<Category?> GetCategoryByIdAsync(int id)
        {
            return await Task.Run(() =>
            {
                SQLiteParameter[] parameters = new SQLiteParameter[]
                {
                    new("@Id", id)
                };

                DataTable dataTable = MainClient.SqlClient.Select(
                    "SELECT * FROM Categories WHERE Id_Category = @Id",
                    parameters);

                return dataTable.Rows.Count == 0 ? null : this.MapDataRowToCategory(dataTable.Rows[0]);
            });
        }

        public async Task<Category> AddCategoryAsync(Category category)
        {
            return await Task.Run(() =>
            {
                SQLiteParameter[] parameters = new SQLiteParameter[]
                {
                    new("@Name", category.Name),
                    new("@Description", category.Description ?? (object)DBNull.Value),
                    new("@Icon", category.Icon ?? (object)DBNull.Value)
                };

                int id = MainClient.SqlClient.Insert(
                    @"INSERT INTO Categories (Name, Description, Icon) 
                      VALUES (@Name, @Description, @Icon)",
                    parameters);

                category.Id = id;
                return category;
            });
        }

        public async Task<Category> UpdateCategoryAsync(Category category)
        {
            return await Task.Run(async () =>
            {
                Category? existingCategory = await this.GetCategoryByIdAsync(category.Id);

                if (existingCategory == null)
                {
                    return null;
                }

                SQLiteParameter[] parameters = new SQLiteParameter[]
                {
                    new("@Id", category.Id),
                    new("@Name", category.Name),
                    new("@Description", category.Description ?? (object)DBNull.Value),
                    new("@Icon", category.Icon ?? (object)DBNull.Value)
                };

                _ = MainClient.SqlClient.Update(
                    @"UPDATE Categories 
                      SET Name = @Name, Description = @Description, Icon = @Icon 
                      WHERE Id_Category = @Id",
                    parameters);

                return category;
            });
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            return await Task.Run(async () =>
            {
                Category? category = await this.GetCategoryByIdAsync(id);

                if (category == null)
                {
                    return false;
                }

                SQLiteParameter[] parameters = new SQLiteParameter[]
                {
                    new("@Id", id)
                };

                int rowsAffected = MainClient.SqlClient.Delete(
                    "DELETE FROM Categories WHERE Id_Category = @Id",
                    parameters);

                return rowsAffected > 0;
            });
        }

        private Category MapDataRowToCategory(DataRow row)
        {
            return new Category
            {
                Id = Convert.ToInt32(row["Id_Category"]),
                Name = row["Name"].ToString(),
                Description = row["Description"] != DBNull.Value ? row["Description"].ToString() : string.Empty,
                Icon = row["Icon"] != DBNull.Value ? row["Icon"].ToString() : string.Empty
            };
        }

        #endregion
    }
}