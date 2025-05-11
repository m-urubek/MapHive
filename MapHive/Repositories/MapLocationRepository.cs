namespace MapHive.Repositories
{
    using System.Data;
    using System.Data.SQLite;
    using MapHive.Models.Exceptions;
    using MapHive.Models.RepositoryModels;
    using MapHive.Services;
    using MapHive.Singletons;
    using MapHive.Utilities;

    public class MapLocationRepository(ISqlClientSingleton sqlClientSingleton, ILogManagerService logManagerService) : IMapLocationRepository
    {
        private readonly ISqlClientSingleton _sqlClientSingleton = sqlClientSingleton;
        private readonly ILogManagerService _logManagerService = logManagerService;

        public async Task<IEnumerable<MapLocationGet>> GetAllLocationsAsync()
        {
            List<MapLocationGet> list = new();
            DataTable dataTable = await _sqlClientSingleton.SelectAsync(query: "SELECT * FROM MapLocations");
            foreach (DataRow row in dataTable.Rows)
            {
                list.Add(item: MapDataRowToGet(row));
            }

            return list;
        }

        public async Task<MapLocationGet?> GetLocationByIdAsync(int id)
        {
            SQLiteParameter[] parameters = [new("@Id_Log", id)];
            DataTable dt = await _sqlClientSingleton.SelectAsync(query: "SELECT * FROM MapLocations WHERE Id_MapLocation = @Id_Log", parameters: parameters);
            return dt.Rows.Count == 0 ? null : MapDataRowToGet(dt.Rows[0]);
        }

        public async Task<MapLocationGet> AddLocationAsync(MapLocationCreate createDto)
        {
            DateTime now = DateTime.UtcNow;
            SQLiteParameter[] parameters =
            [
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
                new("@CategoryId", createDto.CategoryId)
            ];
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
            SQLiteParameter[] parameters =
            [
                new("@Id_Log", updateDto.Id),
                new("@Name", updateDto.Name),
                new("@Description", updateDto.Description),
                new("@Latitude", updateDto.Latitude),
                new("@Longitude", updateDto.Longitude),
                new("@Address", updateDto.Address),
                new("@Website", updateDto.Website),
                new("@PhoneNumber", updateDto.PhoneNumber),
                new("@UpdatedAt", now),
                new("@IsAnonymous", updateDto.IsAnonymous),
                new("@CategoryId", updateDto.CategoryId)
            ];
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
            SQLiteParameter[] p = [new("@Id_Log", id)];
            int rows = await _sqlClientSingleton.DeleteAsync(query: "DELETE FROM MapLocations WHERE Id_MapLocation=@Id_Log", parameters: p);
            return rows > 0;
        }

        public async Task<IEnumerable<MapLocationGet>> GetLocationsByUserIdAsync(int userId)
        {
            List<MapLocationGet> list = new();
            SQLiteParameter[] p = [new("@UserId", userId)];
            DataTable dt = await _sqlClientSingleton.SelectAsync(query: "SELECT * FROM MapLocations WHERE UserId=@UserId", parameters: p);
            foreach (DataRow row in dt.Rows)
            {
                list.Add(item: MapDataRowToGet(row));
            }

            return list;
        }

        public async Task<MapLocationGet?> GetLocationWithCategoryAsync(int id)
        {
            MapLocationGet? loc = await GetLocationByIdAsync(id: id);
            (loc ?? throw new PublicErrorException($"Location \"{id}\" not found in database!")).Category = await GetCategoryByIdAsync(id: loc.CategoryId);
            return loc;
        }

        // Category methods
        public async Task<IEnumerable<CategoryGet>> GetAllCategoriesAsync()
        {
            List<CategoryGet> list = new();
            DataTable dt = await _sqlClientSingleton.SelectAsync(query: "SELECT * FROM Categories");
            foreach (DataRow row in dt.Rows)
            {
                list.Add(item: MapCategoryRow(row));
            }

            return list;
        }

        public async Task<CategoryGet?> GetCategoryByIdAsync(int id)
        {
            SQLiteParameter[] p = [new("@Id_Log", id)];
            DataTable dt = await _sqlClientSingleton.SelectAsync(query: "SELECT * FROM Categories WHERE Id_Category=@Id_Log", parameters: p);
            return dt.Rows.Count == 0 ? null : MapCategoryRow(dt.Rows[0]);
        }

        public async Task<CategoryGet> AddCategoryAsync(CategoryCreate categoryCreate)
        {
            SQLiteParameter[] p =
            [
                new("@Name", categoryCreate.Name),
                new("@Description", categoryCreate.Description),
                new("@Icon", categoryCreate.Icon)
            ];
            int id = await _sqlClientSingleton.InsertAsync(
query: "INSERT INTO Categories (Name, Description, Icon) VALUES (@Name, @Description, @Icon);", parameters: p);
            return new CategoryGet { Id = id, Name = categoryCreate.Name, Description = categoryCreate.Description, Icon = categoryCreate.Icon };
        }

        public async Task<CategoryGet?> UpdateCategoryAsync(CategoryUpdate updateDto)
        {
            SQLiteParameter[] p =
            [
                new("@Id_Log", updateDto.Id),
                new("@Name", updateDto.Name),
                new("@Description", updateDto.Description ?? (object)DBNull.Value),
                new("@Icon", updateDto.Icon ?? (object)DBNull.Value)
            ];
            int rows = await _sqlClientSingleton.UpdateAsync(
query: "UPDATE Categories SET Name=@Name, Description=@Description, Icon=@Icon WHERE Id_Category=@Id_Log", parameters: p);
            return rows > 0 ? await GetCategoryByIdAsync(id: updateDto.Id) : null;
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            SQLiteParameter[] p = [new("@Id_Log", id)];
            int rows = await _sqlClientSingleton.DeleteAsync(query: "DELETE FROM Categories WHERE Id_Category=@Id_Log", parameters: p);
            return rows > 0;
        }

        private MapLocationGet MapDataRowToGet(DataRow row)
        {
            const string table = "MapLocations";
            return new MapLocationGet
            {
                Id = row.GetValueOrDefault(_logManagerService, tableName: table, columnName: "Id_MapLocation", isRequired: true, converter: Convert.ToInt32),
                Name = row.GetValueOrDefault(_logManagerService, tableName: table, columnName: "Name", isRequired: true, converter: v => v.ToString()!, defaultValue: string.Empty),
                Description = row.GetValueOrDefault(_logManagerService, tableName: table, columnName: "Description", isRequired: true, converter: v => v.ToString()!, defaultValue: string.Empty),
                Latitude = row.GetValueOrDefault(_logManagerService, tableName: table, columnName: "Latitude", isRequired: true, converter: Convert.ToDouble),
                Longitude = row.GetValueOrDefault(_logManagerService, tableName: table, columnName: "Longitude", isRequired: true, converter: Convert.ToDouble),
                Address = row.GetValueOrDefault(_logManagerService, tableName: table, columnName: "Address", isRequired: true, converter: v => v.ToString()!, defaultValue: string.Empty),
                Website = row.GetValueOrDefault(_logManagerService, tableName: table, columnName: "Website", isRequired: true, converter: v => v.ToString()!, defaultValue: string.Empty),
                PhoneNumber = row.GetValueOrDefault(_logManagerService, tableName: table, columnName: "PhoneNumber", isRequired: true, converter: v => v.ToString()!, defaultValue: string.Empty),
                CreatedAt = row.GetValueOrDefault(_logManagerService, tableName: table, columnName: "CreatedAt", isRequired: true, converter: Convert.ToDateTime),
                UpdatedAt = row.GetValueOrDefault(_logManagerService, tableName: table, columnName: "UpdatedAt", isRequired: true, converter: Convert.ToDateTime),
                UserId = row.GetValueOrDefault(_logManagerService, tableName: table, columnName: "UserId", isRequired: true, converter: Convert.ToInt32),
                IsAnonymous = row.GetValueOrDefault(_logManagerService, tableName: table, columnName: "IsAnonymous", isRequired: true, converter: Convert.ToBoolean),
                CategoryId = row.GetValueOrDefault(_logManagerService, tableName: table, columnName: "CategoryId", isRequired: true, converter: Convert.ToInt32),
                Category = null // populated by calling method
            };
        }

        private CategoryGet MapCategoryRow(DataRow row)
        {
            const string table = "Category";
            return new CategoryGet
            {
                Id = row.GetValueOrDefault(_logManagerService, tableName: table, columnName: "Id_Category", isRequired: true, converter: Convert.ToInt32),
                Name = row.GetValueOrDefault(_logManagerService, tableName: table, columnName: "Name", isRequired: true, converter: v => v.ToString()!, defaultValue: string.Empty),
                Description = row.GetValueOrDefault(_logManagerService, tableName: table, columnName: "Description", isRequired: false, converter: v => v.ToString()!, defaultValue: string.Empty),
                Icon = row.GetValueOrDefault(_logManagerService, tableName: table, columnName: "Icon", isRequired: false, converter: v => v.ToString()!, defaultValue: string.Empty)
            };
        }
    }
}
