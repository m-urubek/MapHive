namespace MapHive.Repositories;

using System;
using System.Data;
using System.Data.SQLite;
using System.Text.RegularExpressions;
using MapHive.Models.Data.DbTableModels;
using MapHive.Services;
using MapHive.Singletons;

public partial class IpBansRepository(
    ISqlClientSingleton sqlClientSingleton,
    ILogManagerService logManagerService) : IIpBansRepository
{
    private readonly ISqlClientSingleton _sqlClientSingleton = sqlClientSingleton;
    private readonly ILogManagerService _logManagerService = logManagerService;

    public async Task<bool> IsIpBannedAsync(string hashedIpAddress)
    {
        string query = @"
                SELECT 1 FROM IpBans
                WHERE HashedIpAddress = @HashedIpAddress
                  AND (ExpiresAt IS NULL OR ExpiresAt > @Now)
                LIMIT 1";
        SQLiteParameter[] parameters = [
            new("@HashedIpAddress", hashedIpAddress),
            new("@Now", DateTime.UtcNow)
        ];
        DataTable result = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);
        return result.Rows.Count > 0;
    }

    public async Task<int> CreateIpBanAsync(
        string hashedIpAddress,
        string? reason,
        DateTime banCreatedDateTime,
        int bannedByAccountId,
        DateTime? expiresAt)
    {
        if (string.IsNullOrEmpty(value: hashedIpAddress) || !MyRegex().IsMatch(input: hashedIpAddress))
        {
            throw new ArgumentException("IP address must be a valid SHA256 hash.");
        }

        string query = @"
                INSERT INTO IpBans (HashedIpAddress, Reason, CreatedDateTime, ExpiresAt, BannedByAccountId)
                VALUES (@HashedIpAddress, @Reason, @CreatedDateTime, @ExpiresAt, @BannedByAccountId);";
        SQLiteParameter[] parameters = [
            new("@HashedIpAddress", hashedIpAddress),
            new("@Reason", reason),
            new("@CreatedDateTime", banCreatedDateTime.ToString(format: "yyyy-MM-dd HH:mm:ss")),
            new("@ExpiresAt", expiresAt?.ToString(format: "yyyy-MM-dd HH:mm:ss")),
            new("@BannedByAccountId", bannedByAccountId)
        ];
        return await _sqlClientSingleton.InsertAsync(query: query, parameters: parameters);
    }

    public async Task RemoveIpBanAsync(int banId)
    {
        string query = "UPDATE IpBans SET ExpiresAt = @ExpiresAt WHERE Id_IpBans = @Id_IpBans";
        SQLiteParameter[] parameters = [
            new("@ExpiresAt", DateTime.UtcNow),
            new("@Id_IpBans", banId)
        ];
        _ = await _sqlClientSingleton.UpdateOrThrowAsync(query: query, parameters: parameters);
    }

    public async Task<IpBanExtended?> GetActiveIpBanByIpAddressAsync(string hashedIpAddress)
    {
        string query = @"
                SELECT i.*, a.Username
                FROM IpBans i
                LEFT JOIN Accounts a ON i.BannedByAccountId = a.Id_Accounts
                WHERE i.HashedIpAddress = @HashedIpAddress AND (i.ExpiresAt IS NULL OR i.ExpiresAt > @Now)
                ORDER BY i.CreatedDateTime DESC LIMIT 1";
        SQLiteParameter[] parameters = [
            new("@HashedIpAddress", hashedIpAddress),
            new("@Now", DateTime.UtcNow)
        ];
        DataTable result = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);
        return result.Rows.Count > 0 ? MapDataRowToIpBanGet(row: result.Rows[0]) : null;
    }

    private static IpBanExtended MapDataRowToIpBanGet(DataRow row)
    {
        return new IpBanExtended
        {
            Id = row.GetValueThrowNotPresentOrNull<int>(columnName: "Id_IpBans"),
            HashedIpAddress = row.GetValueThrowNotPresentOrNull<string>(columnName: "HashedIpAddress"),
            Reason = row.GetValueThrowNotPresent<string?>(columnName: "Reason"),
            CreatedDateTime = row.GetValueThrowNotPresentOrNull<DateTime>(columnName: "CreatedDateTime"),
            BannedByAccountId = row.GetValueThrowNotPresentOrNull<int>(columnName: "BannedByAccountId"),
            BannedByUsername = row.GetValueThrowNotPresentOrNull<string>(columnName: "Username"),
            ExpiresAt = row.GetValueThrowNotPresent<DateTime?>(columnName: "ExpiresAt")
        };
    }

    [GeneratedRegex("^[a-fA-F0-9]{64}$")]
    private static partial Regex MyRegex();
}
