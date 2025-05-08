using MapHive.Models.RepositoryModels;
using MapHive.Models.ViewModels;

namespace MapHive.Services
{
    public interface IReviewService
    {
        Task<ReviewViewModel> GetCreateModelAsync(int locationId);
        Task<ReviewGet> CreateReviewAsync(ReviewViewModel model, int userId);
        Task<ReviewViewModel> GetEditModelAsync(int reviewId, int userId);
        Task EditReviewAsync(int id, ReviewViewModel model, int userId);
        Task<int> DeleteReviewAsync(int id, int userId, bool isAdmin);
    }
}