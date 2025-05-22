namespace MapHive.Repositories;

using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using MapHive.Models.Data;
using MapHive.Models.Data.DbTableModels;
using MapHive.Models.Enums;
using MapHive.Models.Exceptions;
using MapHive.Singletons;

public class AccountsRepository(ISqlClientSingleton sqlClientSingleton) : IAccountsRepository
{
    private readonly ISqlClientSingleton _sqlClientSingleton = sqlClientSingleton;

    // Create a new user using DTO
    public async Task<int> CreateAccountAsync(
        string username,
        string passwordHash,
        DateTime registrationDate,
        AccountTier tier,
        string ipAddressHistory
    )
    {
        string query = @"
                INSERT into Accounts (Username, PasswordHash, RegistrationDate, Tier, IpAddressHistory)
                VALUES (@Username, @PasswordHash, @RegistrationDate, @Tier, @IpAddressHistory);";

        SQLiteParameter[] parameters =
        [
            new("@Username", username),
            new("@PasswordHash", passwordHash),
            new("@RegistrationDate", registrationDate),
            new("@Tier", tier),
            new("@IpAddressHistory", ipAddressHistory)
        ];

        return await _sqlClientSingleton.InsertAsync(query: query, parameters: parameters);
    }

    // Basic user lookup
    public async Task<AccountAtomic?> GetAccountByIdAsync(int id)
    {
        string query = "SELECT * FROM Accounts WHERE Id_Accounts = @Id_Accounts";
        SQLiteParameter[] parameters = [new("@Id_Accounts", id)];
        DataTable result = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);
        return result.Rows.Count > 0 ? MapDataRowToaccountGet(row: result.Rows[0]) : null;
    }

    public async Task<AccountAtomic> GetAccountByIdOrThrowAsync(int id)
    {
        AccountAtomic? user = await GetAccountByIdAsync(id: id);
        return user ?? throw new PublicErrorException($"User \"{id}\" not found in database!");
    }

    public async Task<AccountAtomic?> GetAccountByUsernameAsync(string username)
    {
        string query = "SELECT * FROM Accounts WHERE Username = @Username";
        SQLiteParameter[] parameters = [new("@Username", username)];
        DataTable result = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);

        if (result.Rows.Count == 0)
        {
            throw new OrangeUserException($"Account \"{username}\" not found");
        }

        AccountAtomic user = MapDataRowToaccountGet(row: result.Rows[0]);

        return string.IsNullOrEmpty(value: user.PasswordHash) ? throw new Exception("User {username} found but has empty password hash") : user;
    }

    public async Task<AccountAtomic> GetAccountByUsernameOrThrowAsync(string username)
    {
        AccountAtomic? user = await GetAccountByUsernameAsync(username: username);
        return user ?? throw new PublicErrorException($"User \"{username}\" not found in database!");
    }

    public async Task<bool> CheckUsernameExistsAsync(string username)
    {
        string query = "SELECT 1 FROM Accounts WHERE Username = @Username LIMIT 1";
        SQLiteParameter[] parameters = [new("@Username", username)];
        DataTable result = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);
        return result.Rows.Count > 0;
    }

    public async Task<bool> IsIpBannedAsync(string hashedIpAddress)
    {
        string query = @"
                SELECT 1 FROM IpBans
                WHERE HashedIpAddress = @HashedIpAddress
                  AND (ExpiresAt IS NULL OR ExpiresAt > @Now)
                LIMIT 1";
        SQLiteParameter[] parameters =
        [
            new("@HashedIpAddress", hashedIpAddress),
            new("@Now", DateTime.UtcNow)
        ];
        DataTable result = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);
        return result.Rows.Count > 0;
    }

    public async Task UpdateAccountOrThrowAsync(
        int id,
        DynamicValue<string> username,
        DynamicValue<string> passwordHash,
        DynamicValue<AccountTier> tier,
        DynamicValue<string> ipAddressHistory)
    {
        await _sqlClientSingleton.UpdateFromUpdateValuesOrThrowAsync(
            tableName: "Accounts",
            pkColumnName: "Id_Accounts",
            pkValue: id,
            updateValuesByColumnNames: new Dictionary<string, DynamicValue<object?>>
            {
                { "Username", username.AsGeneric() },
                { "PasswordHash", passwordHash.AsGeneric() },
                { "Tier", tier.AsGeneric() },
                { "IpAddressHistory", ipAddressHistory.AsGeneric() }
            }
        );
    }

    public async Task<IEnumerable<AccountAtomic>> GeAccountsAsync(string searchTerm, int page, int pageSize, string sortColumnName = "", string sortDirection = "asc")
    {
        // Validate and sanitize sort field/direction
        HashSet<string> validSortFields = new()
        { "Username", "RegistrationDate", "Tier" };
        string orderBy = validSortFields.Contains(item: sortColumnName) ? sortColumnName : "Username";
        string direction = sortDirection.Equals("desc", StringComparison.CurrentCultureIgnoreCase) ? "DESC" : "ASC";

        string whereClause = string.IsNullOrWhiteSpace(value: searchTerm)
            ? ""
            : "WHERE Username LIKE @SearchTerm";

        string query = $@"
                SELECT * FROM Accounts
                {whereClause}
                ORDER BY {orderBy} {direction}
                LIMIT @PageSize OFFSET @Offset";

        List<SQLiteParameter> parametersList = new();
        if (!string.IsNullOrWhiteSpace(value: searchTerm))
        {
            parametersList.Add(item: new SQLiteParameter("@SearchTerm", $"%{searchTerm}%"));
        }

        parametersList.Add(item: new SQLiteParameter("@PageSize", pageSize));
        parametersList.Add(item: new SQLiteParameter("@Offset", (page - 1) * pageSize));

        DataTable result = await _sqlClientSingleton.SelectAsync(query: query, parameters: [.. parametersList]);
        List<AccountAtomic> list = new();
        foreach (DataRow row in result.Rows)
        {
            list.Add(item: MapDataRowToaccountGet(row: row));
        }

        return list;
    }

    public async Task<int> GetTotalAccountsCountAsync(string searchTerm)
    {
        string query = string.IsNullOrWhiteSpace(value: searchTerm)
            ? "SELECT COUNT(*) FROM Accounts"
            : "SELECT COUNT(*) FROM Accounts WHERE Username LIKE @SearchTerm";
        SQLiteParameter[] parameters = string.IsNullOrWhiteSpace(value: searchTerm)
            ? []
            : [new("@SearchTerm", $"%{searchTerm}%")];
        DataTable result = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);
        return Convert.ToInt32(value: result.Rows[0][0]);
    }

    private static AccountAtomic MapDataRowToaccountGet(DataRow row)
    {
        return new AccountAtomic
        {
            Id = row.GetValueThrowNotPresentOrNull<int>(columnName: "Id_Accounts"),
            Username = row.GetValueThrowNotPresentOrNull<string>(columnName: "Username"),
            PasswordHash = row.GetValueThrowNotPresentOrNull<string>(columnName: "PasswordHash"),
            Tier = (AccountTier)row.GetValueThrowNotPresentOrNull<int>(columnName: "Tier"),
            RegistrationDate = row.GetValueThrowNotPresentOrNull<DateTime>(columnName: "RegistrationDate"),
            IpAddressHistory = row.GetValueThrowNotPresentOrNull<string>(columnName: "IpAddressHistory"),
            DarkModeEnabled = row.GetValueThrowNotPresentOrNull<bool>(columnName: "DarkModeEnabled")
        };
    }

    public async Task UpdateDarkModePreferenceAsync(int id, bool enabled)
    {
        string query = "UPDATE Accounts SET DarkModeEnabled = @Enabled WHERE Id_Accounts = @Id";
        SQLiteParameter[] parameters =
        [
            new("@Enabled", enabled ? 1 : 0),
            new("@Id", id)
        ];
        _ = await _sqlClientSingleton.UpdateOrThrowAsync(query: query, parameters: parameters);
    }
}
