namespace MapHive.Repositories;

using System.Data;
using System.Data.SQLite;
using MapHive.Models.Data;
using MapHive.Models.Data.DbTableModels;
using MapHive.Models.Exceptions;
using MapHive.Services;
using MapHive.Singletons;

public class ReviewRepository(
    ISqlClientSingleton sqlClientSingleton,
    IAccountsRepository userRepository,
    ILogManagerService logManagerService) : IReviewRepository
{
    private readonly ISqlClientSingleton _sqlClientSingleton = sqlClientSingleton;
    private readonly IAccountsRepository _userRepository = userRepository;
    private readonly ILogManagerService _logManagerService = logManagerService;

    public async Task<List<ReviewExtended>?> GetReviewsByLocationIdAsync(int locationId)
    {
        string query = @"
                SELECT r.*, u.Username, tm.MessageText
                FROM Reviews r
                LEFT JOIN Accounts u ON r.AuthorId = u.Id_Accounts
                LEFT JOIN DiscussionThreads t ON r.Id_Reviews = t.ReviewId
                LEFT JOIN ThreadMessages tm ON t.Id_DiscussionThreads = tm.ThreadId
                WHERE r.LocationId = @LocationId AND tm.IsInitialMessage = 1
                ORDER BY r.CreatedAt DESC";

        SQLiteParameter[] parameters = [new("@LocationId", locationId)];
        DataTable result = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);

        List<ReviewExtended> reviews = new();
        foreach (DataRow row in result.Rows)
        {
            ReviewExtended review = MapRowToReviewGet(row: row);
            reviews.Add(item: review);
        }
        return reviews;
    }

    public async Task<ReviewExtended> GetReviewByIdOrThrowAsync(int id)
    {
        string query = @"
                SELECT r.*, u.Username, tm.MessageText
                FROM Reviews r
                LEFT JOIN Accounts u ON r.AuthorId = u.Id_Accounts
                LEFT JOIN DiscussionThreads t ON r.Id_Reviews = t.ReviewId
                LEFT JOIN ThreadMessages tm ON t.Id_DiscussionThreads = tm.ThreadId
                WHERE r.Id_Reviews = @id AND tm.IsInitialMessage = 1
                ORDER BY r.CreatedAt DESC";
        SQLiteParameter[] parameters = [new("@id", id)];
        DataTable result = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);

        if (result.Rows.Count == 0)
            throw new PublicWarningException($"Review {id} not found");

        ReviewExtended rg = MapRowToReviewGet(row: result.Rows[0]);
        return rg;
    }

    public async Task<int> CreateReviewAsync(
        int locationId,
        int accountId,
        int rating,
        bool isAnonymous
    )
    {
        DateTime now = DateTime.UtcNow;
        string query = @"
                INSERT INTO Reviews (LocationId, AuthorId, Rating, IsAnonymous, CreatedAt, UpdatedAt)
                VALUES (@LocationId, @AuthorId, @Rating, @IsAnonymous, @CreatedAt, @UpdatedAt);";

        SQLiteParameter[] parameters = [
            new("@LocationId", locationId),
            new("@AuthorId", accountId),
            new("@Rating", rating),
            new("@IsAnonymous", isAnonymous),
            new("@CreatedAt", now),
            new("@UpdatedAt", now)
        ];

        return await _sqlClientSingleton.InsertAsync(query: query, parameters: parameters);
    }

    public async Task UpdateReviewOrThrowAsync(
        int id,
        DynamicValue<int> rating,
        DynamicValue<string> reviewText,
        DynamicValue<bool> isAnonymous
    )
    {
        await _sqlClientSingleton.UpdateFromUpdateValuesOrThrowAsync(
            tableName: "Reviews",
            pkColumnName: "Id_Reviews",
            pkValue: id,
            updateValuesByColumnNames: new Dictionary<string, DynamicValue<object?>>
            {
                ["Rating"] = rating.AsGeneric(),
                ["ReviewText"] = reviewText.AsGeneric(),
                ["IsAnonymous"] = isAnonymous.AsGeneric(),
                ["UpdatedAt"] = DynamicValue<object?>.Set(DateTime.UtcNow)
            });
    }

    public async Task DeleteReviewOrThrowAsync(int id)
    {
        // Consider adding user ID check if only owners can delete
        string query = "DELETE FROM Reviews WHERE Id_Reviews = @Id";
        SQLiteParameter[] parameters = [new("@Id", id)];
        // Use injected _sqlClientSingleton
        int rowsAffected = await _sqlClientSingleton.DeleteAsync(query: query, parameters: parameters);
        if (rowsAffected == 0)
            throw new PublicWarningException($"Review {id} not found");
    }

    public async Task<double> GetAverageRatingForLocationAsync(int locationId)
    {
        string query = "SELECT AVG(Rating) AS AverageRating FROM Reviews WHERE LocationId = @LocationId";
        SQLiteParameter[] parameters = [new("@LocationId", locationId)];
        // Use injected _sqlClientSingleton
        DataTable result = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);

        return result.Rows.Count == 0 || result.Rows[0]["AverageRating"] == DBNull.Value
            ? 0.0 // Return double
            : Convert.ToDouble(value: result.Rows[0]["AverageRating"]);
    }

    public async Task<int> GetReviewCountForLocationAsync(int locationId)
    {
        string query = "SELECT COUNT(*) AS ReviewCount FROM Reviews WHERE LocationId = @LocationId";
        SQLiteParameter[] parameters = [new("@LocationId", locationId)];
        // Use injected _sqlClientSingleton
        DataTable result = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);

        return result.Rows.Count == 0 || result.Rows[0]["ReviewCount"] == DBNull.Value
             ? 0
             : Convert.ToInt32(value: result.Rows[0]["ReviewCount"]);
    }

    public async Task<bool> HasUserReviewedLocationAsync(int accountId, int locationId)
    {
        string query = "SELECT 1 FROM Reviews WHERE AuthorId = @AuthorId AND LocationId = @LocationId LIMIT 1"; // More efficient query
        SQLiteParameter[] parameters = [
            new("@AuthorId", accountId),
            new("@LocationId", locationId)
        ];
        // Use injected _sqlClientSingleton
        DataTable result = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);

        return result.Rows.Count > 0; // Check if any row was returned
    }

    private static ReviewExtended MapRowToReviewGet(DataRow row)
    {
        return new ReviewExtended
        {
            Id = row.GetValueThrowNotPresentOrNull<int>(columnName: "Id_Reviews"),
            LocationId = row.GetValueThrowNotPresentOrNull<int>(columnName: "LocationId"),
            AccountId = row.GetValueThrowNotPresentOrNull<int>(columnName: "AuthorId"),
            Rating = row.GetValueThrowNotPresentOrNull<int>(columnName: "Rating"),
            IsAnonymous = row.GetValueThrowNotPresentOrNull<bool>(columnName: "IsAnonymous"),
            CreatedAt = row.GetValueThrowNotPresentOrNull<DateTime>(columnName: "CreatedAt"),
            UpdatedAt = row.GetValueThrowNotPresentOrNull<DateTime>(columnName: "UpdatedAt"),
            AuthorUsername = row.GetValueThrowNotPresentOrNull<string>(columnName: "Username"),
            ReviewText = row.GetValueThrowNotPresentOrNull<string>(columnName: "MessageText"),
        };
    }
}
