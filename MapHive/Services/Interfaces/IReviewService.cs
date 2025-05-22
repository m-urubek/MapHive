namespace MapHive.Services;

using MapHive.Models.PageModels;

public interface IReviewService
{
    Task<int> CreateReviewAsync(int locationId, ReviewUpdatePageModel model);
    Task<ReviewUpdatePageModel> GetEditModelAsync(int reviewId);
    /// <summary>returns id of location it is assigned to</summary>
    Task<int> EditReviewAsync(int id, ReviewUpdatePageModel model);
    Task<int> DeleteReviewAsync(int id);
}
