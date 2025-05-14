namespace MapHive.Repositories
{
    using System.Data;
    using System.Data.SQLite;
    using MapHive.Models.Exceptions;
    using MapHive.Models.RepositoryModels;
    using MapHive.Services;
    using MapHive.Singletons;
    using MapHive.Utilities.Extensions;

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

        public async Task<MapLocationGet> GetLocationByIdOrThrowAsync(int id)
        {
            MapLocationGet? location = await GetLocationByIdAsync(id: id);
            return location ?? throw new PublicErrorException($"Location \"{id}\" not found in database!");
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
                new("@AccountId", createDto.AccountId),
                new("@IsAnonymous", createDto.IsAnonymous),
                new("@CategoryId", createDto.CategoryId)
            ];
            int id = await _sqlClientSingleton.InsertAsync(
query: @"INSERT INTO MapLocations (Name, Description, Latitude, Longitude, Address, Website, PhoneNumber, CreatedAt, UpdatedAt, AccountId, IsAnonymous, CategoryId)
                  VALUES (@Name, @Description, @Latitude, @Longitude, @Address, @Website, @PhoneNumber, @CreatedAt, @UpdatedAt, @AccountId, @IsAnonymous, @CategoryId);",
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
                AccountId = result.AccountId,
                IsAnonymous = result.IsAnonymous,
                CategoryId = result.CategoryId,
                CategoryName = string.Empty //todo
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

        public async Task<IEnumerable<MapLocationGet>> GetLocationsByAccountIdAsync(int accountId)
        {
            List<MapLocationGet> list = new();
            SQLiteParameter[] p = [new("@AccountId", accountId)];
            DataTable dt = await _sqlClientSingleton.SelectAsync(query: "SELECT * FROM MapLocations WHERE AccountId=@AccountId", parameters: p);
            foreach (DataRow row in dt.Rows)
            {
                list.Add(item: MapDataRowToGet(row));
            }

            return list;
        }

        public async Task<MapLocationGet?> GetLocationWithCategoryAsync(int id)
        {
            MapLocationGet? loc = await GetLocationByIdAsync(id: id);
            (loc ?? throw new PublicErrorException($"Location \"{id}\" not found in database!")).CategoryName = (await GetCategoryByIdOrThrowAsync(id: loc.CategoryId)).Name;
            return loc;
        }

        public async Task<MapLocationGet> GetLocationWithCategoryOrThrowAsync(int id)
        {
            MapLocationGet? locationGet = await GetLocationWithCategoryAsync(id: id);
            return locationGet ?? throw new PublicErrorException($"Location \"{id}\" not found in database!");
        }

        // CategoryName methods
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

        public async Task<CategoryGet> GetCategoryByIdOrThrowAsync(int id)
        {
            return await GetCategoryByIdAsync(id: id) ?? throw new PublicErrorException($"CategoryName \"{id}\" not found in database!");
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
            return new MapLocationGet
            {
                Id = row.GetValueOrThrow<int>(columnName: "Id_MapLocation"),
                Name = row.GetValueOrThrow<string>(columnName: "Name"),
                Description = row.GetValueOrThrow<string>(columnName: "Description"),
                Latitude = row.GetValueOrThrow<double>(columnName: "Latitude"),
                Longitude = row.GetValueOrThrow<double>(columnName: "Longitude"),
                Address = row.GetValueOrThrow<string>(columnName: "Address"),
                Website = row.GetValueOrThrow<string>(columnName: "Website"),
                PhoneNumber = row.GetValueOrThrow<string>(columnName: "PhoneNumber"),
                CreatedAt = row.GetValueOrThrow<DateTime>(columnName: "CreatedAt"),
                UpdatedAt = row.GetValueOrThrow<DateTime>(columnName: "UpdatedAt"),
                AccountId = row.GetValueOrThrow<int>(columnName: "AccountId"),
                IsAnonymous = row.GetValueOrThrow<bool>(columnName: "IsAnonymous"),
                CategoryId = row.GetValueOrThrow<int>(columnName: "CategoryId"),
                CategoryName = string.Empty //todo
            };
        }

        private CategoryGet MapCategoryRow(DataRow row)
        {
            return new CategoryGet
            {
                Id = row.GetValueOrThrow<int>(columnName: "Id_Category"),
                Name = row.GetValueOrThrow<string>(columnName: "Name"),
                Description = row.GetAsNullableString(columnName: "Description"),
                Icon = row.GetAsNullableString(columnName: "Icon")
            };
        }
    }
}
