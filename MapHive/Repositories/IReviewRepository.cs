using MapHive.Models;

namespace MapHive.Repositories
{
    public interface IReviewRepository
    {
        Task<IEnumerable<Review>> GetReviewsByLocationIdAsync(int locationId);
        Task<Review?> GetReviewByIdAsync(int id);
        Task<Review> AddReviewAsync(Review review);
        Task<bool> UpdateReviewAsync(Review review);
        Task<bool> DeleteReviewAsync(int id);
        Task<double> GetAverageRatingForLocationAsync(int locationId);
        Task<int> GetReviewCountForLocationAsync(int locationId);
        Task<bool> HasUserReviewedLocationAsync(int userId, int locationId);
    }
}