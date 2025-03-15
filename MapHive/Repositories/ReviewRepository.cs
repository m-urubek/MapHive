using MapHive.Models;
using System.Data;
using System.Data.SQLite;

namespace MapHive.Repositories
{
    public class ReviewRepository : IReviewRepository
    {
        private readonly IUserRepository _userRepository;

        public ReviewRepository(IUserRepository userRepository)
        {
            this._userRepository = userRepository;
        }

        public async Task<IEnumerable<Review>> GetReviewsByLocationIdAsync(int locationId)
        {
            string query = @"
                SELECT * FROM Reviews 
                WHERE LocationId = @LocationId
                ORDER BY CreatedAt DESC";

            SQLiteParameter[] parameters = { new("@LocationId", locationId) };
            DataTable result = await MainClient.SqlClient.SelectAsync(query, parameters);

            List<Review> reviews = new();
            foreach (DataRow row in result.Rows)
            {
                Review review = this.MapRowToReview(row);

                // Get author name
                int userId = Convert.ToInt32(row["UserId"]);
                string username = await this._userRepository.GetUsernameByIdAsync(userId);
                review.AuthorName = review.IsAnonymous ? "Anonymous" : username;

                reviews.Add(review);
            }

            return reviews;
        }

        public async Task<Review?> GetReviewByIdAsync(int id)
        {
            string query = "SELECT * FROM Reviews WHERE Id_Reviews = @Id";
            SQLiteParameter[] parameters = { new("@Id", id) };
            DataTable result = await MainClient.SqlClient.SelectAsync(query, parameters);

            if (result.Rows.Count == 0)
            {
                return null;
            }

            Review review = this.MapRowToReview(result.Rows[0]);

            // Get author name
            int userId = Convert.ToInt32(result.Rows[0]["UserId"]);
            string username = await this._userRepository.GetUsernameByIdAsync(userId);
            review.AuthorName = review.IsAnonymous ? "Anonymous" : username;

            return review;
        }

        public async Task<Review> AddReviewAsync(Review review)
        {
            string query = @"
                INSERT INTO Reviews (LocationId, UserId, Rating, ReviewText, IsAnonymous, CreatedAt, UpdatedAt)
                VALUES (@LocationId, @UserId, @Rating, @ReviewText, @IsAnonymous, @CreatedAt, @UpdatedAt)";

            SQLiteParameter[] parameters = {
                new("@LocationId", review.LocationId),
                new("@UserId", review.UserId),
                new("@Rating", review.Rating),
                new("@ReviewText", review.ReviewText),
                new("@IsAnonymous", review.IsAnonymous),
                new("@CreatedAt", review.CreatedAt),
                new("@UpdatedAt", review.UpdatedAt)
            };

            int reviewId = await MainClient.SqlClient.InsertAsync(query, parameters);
            review.Id = reviewId;

            // Get author name
            string username = await this._userRepository.GetUsernameByIdAsync(review.UserId);
            review.AuthorName = review.IsAnonymous ? "Anonymous" : username;

            return review;
        }

        public async Task<bool> UpdateReviewAsync(Review review)
        {
            string query = @"
                UPDATE Reviews 
                SET Rating = @Rating, 
                    ReviewText = @ReviewText, 
                    IsAnonymous = @IsAnonymous,
                    UpdatedAt = @UpdatedAt
                WHERE Id_Reviews = @Id AND UserId = @UserId";

            SQLiteParameter[] parameters = {
                new("@Id", review.Id),
                new("@Rating", review.Rating),
                new("@ReviewText", review.ReviewText),
                new("@IsAnonymous", review.IsAnonymous),
                new("@UpdatedAt", DateTime.UtcNow),
                new("@UserId", review.UserId)
            };

            int rowsAffected = await MainClient.SqlClient.UpdateAsync(query, parameters);
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteReviewAsync(int id)
        {
            string query = "DELETE FROM Reviews WHERE Id_Reviews = @Id";
            SQLiteParameter[] parameters = { new("@Id", id) };

            int rowsAffected = await MainClient.SqlClient.DeleteAsync(query, parameters);
            return rowsAffected > 0;
        }

        public async Task<double> GetAverageRatingForLocationAsync(int locationId)
        {
            string query = "SELECT AVG(Rating) AS AverageRating FROM Reviews WHERE LocationId = @LocationId";
            SQLiteParameter[] parameters = { new("@LocationId", locationId) };

            DataTable result = await MainClient.SqlClient.SelectAsync(query, parameters);

            return result.Rows.Count == 0 || result.Rows[0]["AverageRating"] == DBNull.Value
                ? 0
                : Convert.ToDouble(result.Rows[0]["AverageRating"]);
        }

        public async Task<int> GetReviewCountForLocationAsync(int locationId)
        {
            string query = "SELECT COUNT(*) AS ReviewCount FROM Reviews WHERE LocationId = @LocationId";
            SQLiteParameter[] parameters = { new("@LocationId", locationId) };

            DataTable result = await MainClient.SqlClient.SelectAsync(query, parameters);

            return result.Rows.Count == 0 ? 0 : Convert.ToInt32(result.Rows[0]["ReviewCount"]);
        }

        public async Task<bool> HasUserReviewedLocationAsync(int userId, int locationId)
        {
            string query = "SELECT COUNT(*) AS ReviewCount FROM Reviews WHERE UserId = @UserId AND LocationId = @LocationId";
            SQLiteParameter[] parameters = {
                new("@UserId", userId),
                new("@LocationId", locationId)
            };

            DataTable result = await MainClient.SqlClient.SelectAsync(query, parameters);

            if (result.Rows.Count == 0)
            {
                return false;
            }

            int count = Convert.ToInt32(result.Rows[0]["ReviewCount"]);
            return count > 0;
        }

        private Review MapRowToReview(DataRow row)
        {
            return new Review
            {
                Id = Convert.ToInt32(row["Id_Reviews"]),
                LocationId = Convert.ToInt32(row["LocationId"]),
                UserId = Convert.ToInt32(row["UserId"]),
                Rating = Convert.ToInt32(row["Rating"]),
                ReviewText = row["ReviewText"].ToString() ?? string.Empty,
                IsAnonymous = Convert.ToBoolean(row["IsAnonymous"]),
                CreatedAt = Convert.ToDateTime(row["CreatedAt"]),
                UpdatedAt = Convert.ToDateTime(row["UpdatedAt"])
            };
        }
    }
}