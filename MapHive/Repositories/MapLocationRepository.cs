namespace MapHive.Repositories
{
    using System.Data;
    using System.Data.SQLite;
    using MapHive.Models.RepositoryModels;
    using MapHive.Singletons;

    public class MapLocationRepository(ISqlClientSingleton sqlClientSingleton) : IMapLocationRepository
    {
        private readonly ISqlClientSingleton _sqlClientSingleton = sqlClientSingleton;

        public async Task<IEnumerable<MapLocationGet>> GetAllLocationsAsync()
        {
            List<MapLocationGet> list = new();
            DataTable dataTable = await _sqlClientSingleton.SelectAsync(query: "SELECT * FROM MapLocations");
            foreach (DataRow row in dataTable.Rows)
            {
                list.Add(item: MapDataRowToGet(row: row));
            }

            return list;
        }

        public async Task<MapLocationGet?> GetLocationByIdAsync(int id)
        {
            SQLiteParameter[] parameters = new SQLiteParameter[] { new("@Id_Log", id) };
            DataTable dt = await _sqlClientSingleton.SelectAsync(query: "SELECT * FROM MapLocations WHERE Id_MapLocation = @Id_Log", parameters: parameters);
            return dt.Rows.Count == 0 ? null : MapDataRowToGet(row: dt.Rows[0]);
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
                new("@Address", createDto.Address),
                new("@Website", createDto.Website),
                new("@PhoneNumber", createDto.PhoneNumber),
                new("@CreatedAt", now),
                new("@UpdatedAt", now),
                new("@UserId", createDto.UserId),
                new("@IsAnonymous", createDto.IsAnonymous),
                new("@CategoryId", createDto.CategoryId.HasValue ? (object)createDto.CategoryId.Value : DBNull.Value)
            };
            int id = await _sqlClientSingleton.InsertAsync(
query: @"INSERT INTO MapLocations (Name, Description, Latitude, Longitude, Address, Website, PhoneNumber, CreatedAt, UpdatedAt, UserId, IsAnonymous, CategoryId)
                  VALUES (@Name, @Description, @Latitude, @Longitude, @Address, @Website, @PhoneNumber, @CreatedAt, @UpdatedAt, @UserId, @IsAnonymous, @CategoryId);",
parameters: parameters);

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
            MapLocationGet? existing = await GetLocationByIdAsync(id: updateDto.Id);
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
            int rows = await _sqlClientSingleton.UpdateAsync(
query: @"UPDATE MapLocations
                  SET Name=@Name, Description=@Description, Latitude=@Latitude, Longitude=@Longitude,
                      Address=@Address, Website=@Website, PhoneNumber=@PhoneNumber,
                      UpdatedAt=@UpdatedAt, IsAnonymous=@IsAnonymous, CategoryId=@CategoryId
                  WHERE Id_MapLocation=@Id_Log", parameters: parameters);
            return rows > 0 ? await GetLocationByIdAsync(id: updateDto.Id) : null;
        }

        public async Task<bool> DeleteLocationAsync(int id)
        {
            SQLiteParameter[] p = new SQLiteParameter[] { new("@Id_Log", id) };
            int rows = await _sqlClientSingleton.DeleteAsync(query: "DELETE FROM MapLocations WHERE Id_MapLocation=@Id_Log", parameters: p);
            return rows > 0;
        }

        public async Task<IEnumerable<MapLocationGet>> GetLocationsByUserIdAsync(int userId)
        {
            List<MapLocationGet> list = new();
            SQLiteParameter[] p = new SQLiteParameter[] { new("@UserId", userId) };
            DataTable dt = await _sqlClientSingleton.SelectAsync(query: "SELECT * FROM MapLocations WHERE UserId=@UserId", parameters: p);
            foreach (DataRow row in dt.Rows)
            {
                list.Add(item: MapDataRowToGet(row: row));
            }

            return list;
        }

        public async Task<MapLocationGet?> GetLocationWithCategoryAsync(int id)
        {
            MapLocationGet? loc = await GetLocationByIdAsync(id: id);
            if (loc?.CategoryId != null)
            {
                loc.Category = await GetCategoryByIdAsync(id: loc.CategoryId.Value);
            }

            return loc;
        }

        // Category methods
        public async Task<IEnumerable<CategoryGet>> GetAllCategoriesAsync()
        {
            List<CategoryGet> list = new();
            DataTable dt = await _sqlClientSingleton.SelectAsync(query: "SELECT * FROM Categories");
            foreach (DataRow row in dt.Rows)
            {
                list.Add(item: MapCategoryRow(row: row));
            }

            return list;
        }

        public async Task<CategoryGet?> GetCategoryByIdAsync(int id)
        {
            SQLiteParameter[] p = new SQLiteParameter[] { new("@Id_Log", id) };
            DataTable dt = await _sqlClientSingleton.SelectAsync(query: "SELECT * FROM Categories WHERE Id_Category=@Id_Log", parameters: p);
            return dt.Rows.Count == 0 ? null : MapCategoryRow(row: dt.Rows[0]);
        }

        public async Task<CategoryGet> AddCategoryAsync(CategoryCreate categoryCreate)
        {
            SQLiteParameter[] p = new SQLiteParameter[]
            {
                new("@Name", categoryCreate.Name),
                new("@Description", categoryCreate.Description),
                new("@Icon", categoryCreate.Icon)
            };
            int id = await _sqlClientSingleton.InsertAsync(
query: @"INSERT INTO Categories (Name, Description, Icon) VALUES (@Name, @Description, @Icon);", parameters: p);
            return new CategoryGet { Id = id, Name = categoryCreate.Name, Description = categoryCreate.Description, Icon = categoryCreate.Icon };
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
            int rows = await _sqlClientSingleton.UpdateAsync(
query: @"UPDATE Categories SET Name=@Name, Description=@Description, Icon=@Icon WHERE Id_Category=@Id_Log", parameters: p);
            return rows > 0 ? await GetCategoryByIdAsync(id: updateDto.Id) : null;
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            SQLiteParameter[] p = new SQLiteParameter[] { new("@Id_Log", id) };
            int rows = await _sqlClientSingleton.DeleteAsync(query: "DELETE FROM Categories WHERE Id_Category=@Id_Log", parameters: p);
            return rows > 0;
        }

        private static MapLocationGet MapDataRowToGet(DataRow row)
        {
            return new MapLocationGet
            {
                Id = Convert.ToInt32(value: row["Id_MapLocation"]),
                Name = row["Name"].ToString() ?? string.Empty,
                Description = row["Description"].ToString() ?? string.Empty,
                Latitude = Convert.ToDouble(value: row["Latitude"]),
                Longitude = Convert.ToDouble(value: row["Longitude"]),
                Address = row["Address"] != DBNull.Value ? (string)row["Address"] : string.Empty,
                Website = row["Website"] != DBNull.Value ? (string)row["Website"] : string.Empty,
                PhoneNumber = row["PhoneNumber"] != DBNull.Value ? (string)row["PhoneNumber"] : string.Empty,
                CreatedAt = Convert.ToDateTime(value: row["CreatedAt"]),
                UpdatedAt = Convert.ToDateTime(value: row["UpdatedAt"]),
                UserId = Convert.ToInt32(value: row["UserId"]),
                IsAnonymous = row["IsAnonymous"] != DBNull.Value && Convert.ToBoolean(value: row["IsAnonymous"]),
                CategoryId = row["CategoryId"] != DBNull.Value ? Convert.ToInt32(value: row["CategoryId"]) : (int?)null,
                Category = null // populated by calling method
            };
        }

        private static CategoryGet MapCategoryRow(DataRow row)
        {
            return new CategoryGet
            {
                Id = Convert.ToInt32(value: row["Id_Category"]),
                Name = row["Name"].ToString() ?? string.Empty,
                Description = row["Description"] != DBNull.Value ? (string)row["Description"] : string.Empty,
                Icon = row["Icon"] != DBNull.Value ? (string)row["Icon"] : string.Empty
            };
        }
    }
}
