using MapHive.Models.RepositoryModels;

namespace MapHive.Repositories
{
    public interface IReviewRepository
    {
        Task<IEnumerable<ReviewGet>> GetReviewsByLocationIdAsync(int locationId);
        Task<ReviewGet?> GetReviewByIdAsync(int id);
        Task<ReviewGet> AddReviewAsync(ReviewCreate review);
        Task<bool> UpdateReviewAsync(ReviewUpdate review);
        Task<bool> DeleteReviewAsync(int id);
        Task<double> GetAverageRatingForLocationAsync(int locationId);
        Task<int> GetReviewCountForLocationAsync(int locationId);
        Task<bool> HasUserReviewedLocationAsync(int userId, int locationId);
    }
}