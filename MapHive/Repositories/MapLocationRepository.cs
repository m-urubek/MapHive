namespace MapHive.Repositories;

using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using MapHive.Models.Data;
using MapHive.Models.Data.DbTableModels;
using MapHive.Models.Exceptions;
using MapHive.Singletons;

public class MapLocationRepository(ISqlClientSingleton _sqlClientSingleton) : IMapLocationRepository
{
    public async Task<IEnumerable<LocationExtended>> GetAllLocationsAsync()
    {
        List<LocationExtended> list = new();
        string query = """
            SELECT L.*, C.Name AS CategoryName, u.Username
            FROM MapLocations L
            LEFT JOIN Categories C ON L.CategoryId = C.Id_Categories
            LEFT JOIN Accounts u ON L.OwnerId = u.Id_Accounts
        """;
        DataTable dataTable = await _sqlClientSingleton.SelectAsync(query: query);
        foreach (DataRow row in dataTable.Rows)
        {
            list.Add(item: MapLocationExtended(row));
        }

        return list;
    }

    public async Task<LocationExtended?> GetLocationByIdAsync(int id)
    {
        SQLiteParameter[] parameters = [new("@Id", id)];
        string query = """
            SELECT L.*, C.Name AS CategoryName, u.Username
            FROM MapLocations L
            LEFT JOIN Categories C ON L.CategoryId = C.Id_Categories
            LEFT JOIN Accounts u ON L.OwnerId = u.Id_Accounts
            WHERE L.Id_MapLocations = @Id
        """;
        DataTable dt = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);
        return dt.Rows.Count == 0 ? null : MapLocationExtended(dt.Rows[0]);
    }

    public async Task<LocationExtended> GetLocationByIdOrThrowAsync(int id)
    {
        LocationExtended? location = await GetLocationByIdAsync(id: id);
        return location ?? throw new PublicErrorException($"Location \"{id}\" not found in database!");
    }

    public async Task<int> CreateLocationAsync(
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
    )
    {
        DateTime now = DateTime.UtcNow;
        SQLiteParameter[] parameters =
        [
            new("@Name", name),
            new("@Description", description),
            new("@Latitude", latitude),
            new("@Longitude", longitude),
            new("@Address", address),
            new("@Website", website),
            new("@PhoneNumber", phoneNumber),
            new("@CreatedAt", now),
            new("@UpdatedAt", now),
            new("@OwnerId", ownerId),
            new("@IsAnonymous", isAnonymous),
            new("@CategoryId", categoryId)
        ];
        int id = await _sqlClientSingleton.InsertAsync(
query: @"INSERT INTO MapLocations (Name, Description, Latitude, Longitude, Address, Website, PhoneNumber, CreatedAt, UpdatedAt, OwnerId, IsAnonymous, CategoryId)
                  VALUES (@Name, @Description, @Latitude, @Longitude, @Address, @Website, @PhoneNumber, @CreatedAt, @UpdatedAt, @OwnerId, @IsAnonymous, @CategoryId);",
parameters: parameters);

        return id;
    }

    public async Task UpdateLocationOrThrowAsync(
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
    )
    {
        await _sqlClientSingleton.UpdateFromUpdateValuesOrThrowAsync(
            tableName: "MapLocations",
            pkColumnName: "Id_MapLocations",
            pkValue: id,
            updateValuesByColumnNames: new Dictionary<string, DynamicValue<object?>>()
            {
                ["Name"] = name.AsGeneric(),
                ["Description"] = description.AsGeneric(),
                ["Latitude"] = latitude.AsGeneric(),
                ["Longitude"] = longitude.AsGeneric(),
                ["Address"] = address.AsGeneric(),
                ["Website"] = website.AsGeneric(),
                ["PhoneNumber"] = phoneNumber.AsGeneric(),
                ["UpdatedAt"] = DynamicValue<object?>.Set(DateTime.UtcNow),
                ["IsAnonymous"] = isAnonymous.AsGeneric(),
                ["CategoryId"] = categoryId.AsGeneric()
            }
        );
    }

    public async Task<bool> DeleteLocationAsync(int id)
    {
        SQLiteParameter[] p = [new("@Id", id)];
        int rows = await _sqlClientSingleton.DeleteAsync(query: "DELETE FROM MapLocations WHERE Id_MapLocations=@Id", parameters: p);
        return rows > 0;
    }

    public async Task<IEnumerable<LocationExtended>> GetLocationsByOwnerIdAsync(int ownerId)
    {
        List<LocationExtended> list = new();
        SQLiteParameter[] p = [new("@OwnerId", ownerId)];
        string query = """
            SELECT L.*, C.Name AS CategoryName, u.Username
            FROM MapLocations L
            LEFT JOIN Categories C ON L.CategoryId = C.Id_Categories
            LEFT JOIN Accounts u ON L.OwnerId = u.Id_Accounts
            WHERE OwnerId=@OwnerId
        """;
        DataTable dt = await _sqlClientSingleton.SelectAsync(query: query, parameters: p);
        foreach (DataRow row in dt.Rows)
        {
            list.Add(item: MapLocationExtended(row));
        }

        return list;
    }

    private static LocationExtended MapLocationExtended(DataRow row)
    {
        return new LocationExtended
        {
            Id = row.GetValueThrowNotPresentOrNull<int>(columnName: "Id_MapLocations"),
            Name = row.GetValueThrowNotPresentOrNull<string>(columnName: "Name"),
            Description = row.GetValueThrowNotPresent<string?>(columnName: "Description"),
            Latitude = row.GetValueThrowNotPresentOrNull<double>(columnName: "Latitude"),
            Longitude = row.GetValueThrowNotPresentOrNull<double>(columnName: "Longitude"),
            Address = row.GetValueThrowNotPresent<string?>(columnName: "Address"),
            Website = row.GetValueThrowNotPresent<string?>(columnName: "Website"),
            PhoneNumber = row.GetValueThrowNotPresent<string?>(columnName: "PhoneNumber"),
            CreatedAt = row.GetValueThrowNotPresentOrNull<DateTime>(columnName: "CreatedAt"),
            UpdatedAt = row.GetValueThrowNotPresentOrNull<DateTime>(columnName: "UpdatedAt"),
            OwnerId = row.GetValueThrowNotPresentOrNull<int>(columnName: "OwnerId"),
            IsAnonymous = row.GetValueThrowNotPresentOrNull<bool>(columnName: "IsAnonymous"),
            CategoryId = row.GetValueThrowNotPresentOrNull<int>(columnName: "CategoryId"),
            CategoryName = row.GetValueThrowNotPresentOrNull<string>(columnName: "CategoryName"),
            OwnerUsername = row.GetValueThrowNotPresentOrNull<string>(columnName: "Username")
        };
    }

    public async Task<IEnumerable<CategoryAtomic>> GetAllCategoriesAsync()
    {
        List<CategoryAtomic> list = new();
        DataTable dt = await _sqlClientSingleton.SelectAsync(query: "SELECT * FROM Categories");
        foreach (DataRow row in dt.Rows)
        {
            list.Add(item: MapCategoryRow(row));
        }

        return list;
    }

    private static CategoryAtomic MapCategoryRow(DataRow row)
    {
        return new CategoryAtomic
        {
            Id = row.GetValueThrowNotPresentOrNull<int>(columnName: "Id_Categories"),
            Name = row.GetValueThrowNotPresentOrNull<string>(columnName: "Name"),
            Description = row.GetValueThrowNotPresent<string?>(columnName: "Description"),
            Icon = row.GetValueThrowNotPresent<string?>(columnName: "Icon")
        };
    }
}
