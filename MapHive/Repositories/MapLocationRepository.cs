using MapHive.Models.RepositoryModels;
using MapHive.Singletons;
using System.Data;
using System.Data.SQLite;

namespace MapHive.Repositories
{
    public class MapLocationRepository : IMapLocationRepository
    {
        private readonly ISqlClientSingleton _sqlClient;

        public MapLocationRepository(ISqlClientSingleton sqlClient)
        {
            this._sqlClient = sqlClient;
        }

        public async Task<IEnumerable<MapLocationGet>> GetAllLocationsAsync()
        {
            List<MapLocationGet> list = new();
            DataTable dataTable = await this._sqlClient.SelectAsync("SELECT * FROM MapLocations");
            foreach (DataRow row in dataTable.Rows)
            {
                list.Add(this.MapDataRowToGet(row));
            }

            return list;
        }

        public async Task<MapLocationGet?> GetLocationByIdAsync(int id)
        {
            SQLiteParameter[] parameters = new SQLiteParameter[] { new("@Id_Log", id) };
            DataTable dt = await this._sqlClient.SelectAsync("SELECT * FROM MapLocations WHERE Id_MapLocation = @Id_Log", parameters);
            return dt.Rows.Count == 0 ? null : this.MapDataRowToGet(dt.Rows[0]);
        }

        public async Task<MapLocationGet> AddLocationAsync(MapLocationCreate createDto)
        {
            DateTime now = DateTime.UtcNow;
            SQLiteParameter[] parameters = new SQLiteParameter[]
            {
                new("@Name", createDto.Name),
                new("@Description", createDto.Description),
                new("@Latitude", createDto.Latitude),
                new("@Longitude", createDto.Longitude),
                new("@Address", createDto.Address ?? (object)DBNull.Value),
                new("@Website", createDto.Website ?? (object)DBNull.Value),
                new("@PhoneNumber", createDto.PhoneNumber ?? (object)DBNull.Value),
                new("@CreatedAt", now),
                new("@UpdatedAt", now),
                new("@UserId", createDto.UserId),
                new("@IsAnonymous", createDto.IsAnonymous),
                new("@CategoryId", createDto.CategoryId.HasValue ? (object)createDto.CategoryId.Value : DBNull.Value)
            };
            int id = await this._sqlClient.InsertAsync(
                @"INSERT INTO MapLocations (Name, Description, Latitude, Longitude, Address, Website, PhoneNumber, CreatedAt, UpdatedAt, UserId, IsAnonymous, CategoryId)
                  VALUES (@Name, @Description, @Latitude, @Longitude, @Address, @Website, @PhoneNumber, @CreatedAt, @UpdatedAt, @UserId, @IsAnonymous, @CategoryId);",
                parameters);

            MapLocationCreate result = createDto; // reuse fields
            return new MapLocationGet
            {
                Id = id,
                Name = result.Name,
                Description = result.Description,
                Latitude = result.Latitude,
                Longitude = result.Longitude,
                Address = result.Address,
                Website = result.Website,
                PhoneNumber = result.PhoneNumber,
                CreatedAt = now,
                UpdatedAt = now,
                UserId = result.UserId,
                IsAnonymous = result.IsAnonymous,
                CategoryId = result.CategoryId
            };
        }

        public async Task<MapLocationGet?> UpdateLocationAsync(MapLocationUpdate updateDto)
        {
            MapLocationGet? existing = await this.GetLocationByIdAsync(updateDto.Id);
            if (existing == null)
            {
                return null;
            }

            DateTime now = DateTime.UtcNow;
            SQLiteParameter[] parameters = new SQLiteParameter[]
            {
                new("@Id_Log", updateDto.Id),
                new("@Name", updateDto.Name),
                new("@Description", updateDto.Description),
                new("@Latitude", updateDto.Latitude),
                new("@Longitude", updateDto.Longitude),
                new("@Address", updateDto.Address ?? (object)DBNull.Value),
                new("@Website", updateDto.Website ?? (object)DBNull.Value),
                new("@PhoneNumber", updateDto.PhoneNumber ?? (object)DBNull.Value),
                new("@UpdatedAt", now),
                new("@IsAnonymous", updateDto.IsAnonymous),
                new("@CategoryId", updateDto.CategoryId.HasValue ? (object)updateDto.CategoryId.Value : DBNull.Value)
            };
            int rows = await this._sqlClient.UpdateAsync(
                @"UPDATE MapLocations
                  SET Name=@Name, Description=@Description, Latitude=@Latitude, Longitude=@Longitude,
                      Address=@Address, Website=@Website, PhoneNumber=@PhoneNumber,
                      UpdatedAt=@UpdatedAt, IsAnonymous=@IsAnonymous, CategoryId=@CategoryId
                  WHERE Id_MapLocation=@Id_Log", parameters);
            return rows > 0 ? await this.GetLocationByIdAsync(updateDto.Id) : null;
        }

        public async Task<bool> DeleteLocationAsync(int id)
        {
            SQLiteParameter[] p = new SQLiteParameter[] { new("@Id_Log", id) };
            int rows = await this._sqlClient.DeleteAsync("DELETE FROM MapLocations WHERE Id_MapLocation=@Id_Log", p);
            return rows > 0;
        }

        public async Task<IEnumerable<MapLocationGet>> GetLocationsByUserIdAsync(int userId)
        {
            List<MapLocationGet> list = new();
            SQLiteParameter[] p = new SQLiteParameter[] { new("@UserId", userId) };
            DataTable dt = await this._sqlClient.SelectAsync("SELECT * FROM MapLocations WHERE UserId=@UserId", p);
            foreach (DataRow row in dt.Rows)
            {
                list.Add(this.MapDataRowToGet(row));
            }

            return list;
        }

        public async Task<MapLocationGet?> GetLocationWithCategoryAsync(int id)
        {
            MapLocationGet? loc = await this.GetLocationByIdAsync(id);
            if (loc?.CategoryId != null)
            {
                loc.Category = await this.GetCategoryByIdAsync(loc.CategoryId.Value);
            }

            return loc;
        }

        // Category methods
        public async Task<IEnumerable<CategoryGet>> GetAllCategoriesAsync()
        {
            List<CategoryGet> list = new();
            DataTable dt = await this._sqlClient.SelectAsync("SELECT * FROM Categories");
            foreach (DataRow row in dt.Rows)
            {
                list.Add(this.MapCategoryRow(row));
            }

            return list;
        }

        public async Task<CategoryGet?> GetCategoryByIdAsync(int id)
        {
            SQLiteParameter[] p = new SQLiteParameter[] { new("@Id_Log", id) };
            DataTable dt = await this._sqlClient.SelectAsync("SELECT * FROM Categories WHERE Id_Category=@Id_Log", p);
            return dt.Rows.Count == 0 ? null : this.MapCategoryRow(dt.Rows[0]);
        }

        public async Task<CategoryGet> AddCategoryAsync(CategoryCreate createDto)
        {
            SQLiteParameter[] p = new SQLiteParameter[]
            {
                new("@Name", createDto.Name),
                new("@Description", createDto.Description ?? (object)DBNull.Value),
                new("@Icon", createDto.Icon ?? (object)DBNull.Value)
            };
            int id = await this._sqlClient.InsertAsync(
                @"INSERT INTO Categories (Name, Description, Icon) VALUES (@Name, @Description, @Icon);", p);
            return new CategoryGet { Id = id, Name = createDto.Name, Description = createDto.Description, Icon = createDto.Icon };
        }

        public async Task<CategoryGet?> UpdateCategoryAsync(CategoryUpdate updateDto)
        {
            SQLiteParameter[] p = new SQLiteParameter[]
            {
                new("@Id_Log", updateDto.Id),
                new("@Name", updateDto.Name),
                new("@Description", updateDto.Description ?? (object)DBNull.Value),
                new("@Icon", updateDto.Icon ?? (object)DBNull.Value)
            };
            int rows = await this._sqlClient.UpdateAsync(
                @"UPDATE Categories SET Name=@Name, Description=@Description, Icon=@Icon WHERE Id_Category=@Id_Log", p);
            return rows > 0 ? await this.GetCategoryByIdAsync(updateDto.Id) : null;
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            SQLiteParameter[] p = new SQLiteParameter[] { new("@Id_Log", id) };
            int rows = await this._sqlClient.DeleteAsync("DELETE FROM Categories WHERE Id_Category=@Id_Log", p);
            return rows > 0;
        }

        private MapLocationGet MapDataRowToGet(DataRow row)
        {
            return new MapLocationGet
            {
                Id = Convert.ToInt32(row["Id_MapLocation"]),
                Name = row["Name"].ToString() ?? string.Empty,
                Description = row["Description"].ToString() ?? string.Empty,
                Latitude = Convert.ToDouble(row["Latitude"]),
                Longitude = Convert.ToDouble(row["Longitude"]),
                Address = row["Address"] != DBNull.Value ? row["Address"].ToString() : string.Empty,
                Website = row["Website"] != DBNull.Value ? row["Website"].ToString() : string.Empty,
                PhoneNumber = row["PhoneNumber"] != DBNull.Value ? row["PhoneNumber"].ToString() : string.Empty,
                CreatedAt = Convert.ToDateTime(row["CreatedAt"]),
                UpdatedAt = Convert.ToDateTime(row["UpdatedAt"]),
                UserId = Convert.ToInt32(row["UserId"]),
                IsAnonymous = row["IsAnonymous"] != DBNull.Value && Convert.ToBoolean(row["IsAnonymous"]),
                CategoryId = row["CategoryId"] != DBNull.Value ? Convert.ToInt32(row["CategoryId"]) : (int?)null,
                Category = null // populated by calling method
            };
        }

        private CategoryGet MapCategoryRow(DataRow row)
        {
            return new CategoryGet
            {
                Id = Convert.ToInt32(row["Id_Category"]),
                Name = row["Name"].ToString() ?? string.Empty,
                Description = row["Description"] != DBNull.Value ? row["Description"].ToString() : string.Empty,
                Icon = row["Icon"] != DBNull.Value ? row["Icon"].ToString() : string.Empty
            };
        }
    }
}