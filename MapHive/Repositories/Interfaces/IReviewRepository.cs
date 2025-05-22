namespace MapHive.Repositories;

using MapHive.Models.Data;
using MapHive.Models.Data.DbTableModels;

public interface IReviewRepository
{
    Task<List<ReviewExtended>?> GetReviewsByLocationIdAsync(int locationId);
    Task<ReviewExtended> GetReviewByIdOrThrowAsync(int id);
    Task<int> CreateReviewAsync(
        int locationId,
        int accountId,
        int rating,
        bool isAnonymous
    );
    public Task UpdateReviewOrThrowAsync(
        int id,
        DynamicValue<int> rating,
        DynamicValue<string> reviewText,
        DynamicValue<bool> isAnonymous
    );
    Task DeleteReviewOrThrowAsync(int id);
    Task<double> GetAverageRatingForLocationAsync(int locationId);
    Task<int> GetReviewCountForLocationAsync(int locationId);
    Task<bool> HasUserReviewedLocationAsync(int accountId, int locationId);
}
