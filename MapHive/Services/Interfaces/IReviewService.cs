namespace MapHive.Services
{
    using MapHive.Models.RepositoryModels;
    using MapHive.Models.ViewModels;

    public interface IReviewService
    {
        Task<ReviewViewModel> GetCreateModelAsync(int locationId);
        Task<ReviewGet> CreateReviewAsync(ReviewViewModel model, int accountId);
        Task<ReviewViewModel> GetEditModelAsync(int reviewId, int accountId);
        Task EditReviewAsync(int id, ReviewViewModel model, int accountId);
        Task<int> DeleteReviewAsync(int id, int accountId);
    }
}