namespace MapHive.Repositories;

using System;
using System.Data;
using System.Data.SQLite;
using System.Threading.Tasks;
using MapHive.Models.Data.DbTableModels;
using MapHive.Services;
using MapHive.Singletons;

public partial class AccountBansRepository(
    ISqlClientSingleton sqlClientSingleton,
    ILogManagerService logManagerService) : IAccountBansRepository
{
    private readonly ISqlClientSingleton _sqlClientSingleton = sqlClientSingleton;
    private readonly ILogManagerService _logManagerService = logManagerService;

    public async Task<int> CreateAccountBanAsync(int accountId, int bannedByAccountId, string? reason, DateTime banCreatedDateTime, DateTime? expiresAt)
    {
        string query = @"
                INSERT INTO AccountBans (AccountId, CreatedDateTime, ExpiresAt, Reason, BannedByAccountId)
                VALUES (@AccountId, @CreatedDateTime, @ExpiresAt, @Reason, @BannedByAccountId);";
        SQLiteParameter[] parameters = [
            new("@AccountId", accountId),
            new("@CreatedDateTime", banCreatedDateTime.ToString("o")),
            new("@ExpiresAt", expiresAt?.ToString("o") ?? null),
            new("@Reason", reason),
            new("@BannedByAccountId", bannedByAccountId)
        ];
        return await _sqlClientSingleton.InsertAsync(query: query, parameters: parameters);
    }

    public async Task RemoveAccountBanAsync(int banId)
    {
        string query = "UPDATE AccountBans SET ExpiresAt = @ExpiresAt WHERE Id_AccountBans = @Id_AccountBans";
        SQLiteParameter[] parameters = [
            new("@ExpiresAt", DateTime.UtcNow),
            new("@Id_AccountBans", banId)
        ];
        _ = await _sqlClientSingleton.UpdateOrThrowAsync(query: query, parameters: parameters);
    }

    public async Task<AccountBanExtended?> GetActiveAccountBanByAccountIdAsync(int accountId)
    {
        string query = @"
                SELECT ab.*, a.Username
                FROM AccountBans ab
                JOIN Accounts a ON ab.BannedByAccountId = a.Id_Accounts
                WHERE ab.AccountId = @AccountId AND (ab.ExpiresAt IS NULL OR ab.ExpiresAt > @Now)
                ORDER BY ab.CreatedDateTime DESC LIMIT 1";
        SQLiteParameter[] parameters = [new("@AccountId", accountId), new("@Now", DateTime.UtcNow)];
        DataTable result = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);
        return result.Rows.Count > 0 ? MapDataRowToAccountBanGet(row: result.Rows[0]) : null;
    }

    public async Task<AccountBanExtended?> GetActiveAccountBanByUsernameAsync(string username)
    {
        string query = @" 
                SELECT AccountBans.*, a.Username
                FROM AccountBans
                JOIN Accounts a ON AccountBans.BannedByAccountId = a.Id_Accounts
                WHERE AccountBans.Username = @Username AND (AccountBans.ExpiresAt IS NULL OR AccountBans.ExpiresAt > @Now)
                ORDER BY AccountBans.CreatedDateTime DESC LIMIT 1";
        SQLiteParameter[] parameters = [new("@Username", username), new("@Now", DateTime.UtcNow)];
        DataTable result = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);
        return result.Rows.Count > 0 ? MapDataRowToAccountBanGet(row: result.Rows[0]) : null;
    }

    private static AccountBanExtended MapDataRowToAccountBanGet(DataRow row)
    {
        return new AccountBanExtended
        {
            Id = row.GetValueThrowNotPresentOrNull<int>(columnName: "Id_AccountBans"),
            AccountId = row.GetValueThrowNotPresentOrNull<int>(columnName: "AccountId"),
            BannedByAccountId = row.GetValueThrowNotPresentOrNull<int>(columnName: "BannedByAccountId"),
            Reason = row.GetValueThrowNotPresent<string?>(columnName: "Reason"),
            CreatedDateTime = row.GetValueThrowNotPresentOrNull<DateTime>(columnName: "CreatedDateTime"),
            ExpiresAt = row.GetValueThrowNotPresent<DateTime?>(columnName: "ExpiresAt"),
            BannedByUsername = row.GetValueThrowNotPresentOrNull<string>(columnName: "Username"),
        };
    }
}
