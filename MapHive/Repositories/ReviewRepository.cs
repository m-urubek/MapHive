using MapHive.Models.RepositoryModels;
using MapHive.Singletons;
using System.Data;
using System.Data.SQLite;

namespace MapHive.Repositories
{
    public class ReviewRepository : IReviewRepository
    {
        private readonly ISqlClientSingleton _sqlClient;
        private readonly IUserRepository _userRepository;

        public ReviewRepository(ISqlClientSingleton sqlClient, IUserRepository userRepository)
        {
            this._sqlClient = sqlClient;
            this._userRepository = userRepository;
        }

        public async Task<IEnumerable<ReviewGet>> GetReviewsByLocationIdAsync(int locationId)
        {
            string query = @"
                SELECT r.*, u.Username
                FROM Reviews r
                LEFT JOIN Users u ON r.UserId = u.Id_User
                WHERE r.LocationId = @LocationId
                ORDER BY r.CreatedAt DESC";

            SQLiteParameter[] parameters = { new("@LocationId", locationId) };
            DataTable result = await this._sqlClient.SelectAsync(query, parameters);

            List<ReviewGet> reviews = new();
            foreach (DataRow row in result.Rows)
            {
                ReviewGet rg = this.MapRowToReviewGet(row);
                string? username = row.Table.Columns.Contains("Username") && row["Username"] != DBNull.Value
                                     ? row["Username"].ToString()
                                     : await this._userRepository.GetUsernameByIdAsync(rg.UserId);
                rg.AuthorName = rg.IsAnonymous ? "Anonymous" : (username ?? "Unknown UserLogin");
                reviews.Add(rg);
            }

            return reviews;
        }

        public async Task<ReviewGet?> GetReviewByIdAsync(int id)
        {
            string query = "SELECT * FROM Reviews WHERE Id_Reviews = @Id";
            SQLiteParameter[] parameters = { new("@Id", id) };
            DataTable result = await this._sqlClient.SelectAsync(query, parameters);

            if (result.Rows.Count == 0)
            {
                return null;
            }

            ReviewGet rg = this.MapRowToReviewGet(result.Rows[0]);
            string username = await this._userRepository.GetUsernameByIdAsync(rg.UserId);
            rg.AuthorName = rg.IsAnonymous ? "Anonymous" : (username ?? "Unknown UserLogin");

            return rg;
        }

        public async Task<ReviewGet> AddReviewAsync(ReviewCreate review)
        {
            DateTime now = DateTime.UtcNow;
            string query = @"
                INSERT INTO Reviews (LocationId, UserId, Rating, ReviewText, IsAnonymous, CreatedAt, UpdatedAt)
                VALUES (@LocationId, @UserId, @Rating, @ReviewText, @IsAnonymous, @CreatedAt, @UpdatedAt);";

            SQLiteParameter[] parameters = {
                new("@LocationId", review.LocationId),
                new("@UserId", review.UserId),
                new("@Rating", review.Rating),
                new("@ReviewText", review.ReviewText),
                new("@IsAnonymous", review.IsAnonymous),
                new("@CreatedAt", now),
                new("@UpdatedAt", now)
            };

            int reviewId = await this._sqlClient.InsertAsync(query, parameters);
            string username = await this._userRepository.GetUsernameByIdAsync(review.UserId);

            return new ReviewGet
            {
                Id = reviewId,
                LocationId = review.LocationId,
                UserId = review.UserId,
                Rating = review.Rating,
                ReviewText = review.ReviewText,
                IsAnonymous = review.IsAnonymous,
                CreatedAt = now,
                UpdatedAt = now,
                AuthorName = review.IsAnonymous ? "Anonymous" : (username ?? "Unknown UserLogin")
            };
        }

        public async Task<bool> UpdateReviewAsync(ReviewUpdate review)
        {
            DateTime now = DateTime.UtcNow;
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
                new("@UpdatedAt", now),
                new("@UserId", review.UserId)
            };

            int rowsAffected = await this._sqlClient.UpdateAsync(query, parameters);
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteReviewAsync(int id)
        {
            // Consider adding user ID check if only owners can delete
            string query = "DELETE FROM Reviews WHERE Id_Reviews = @Id";
            SQLiteParameter[] parameters = { new("@Id", id) };
            // Use injected _sqlClient
            int rowsAffected = await this._sqlClient.DeleteAsync(query, parameters);
            return rowsAffected > 0;
        }

        public async Task<double> GetAverageRatingForLocationAsync(int locationId)
        {
            string query = "SELECT AVG(Rating) AS AverageRating FROM Reviews WHERE LocationId = @LocationId";
            SQLiteParameter[] parameters = { new("@LocationId", locationId) };
            // Use injected _sqlClient
            DataTable result = await this._sqlClient.SelectAsync(query, parameters);

            return result.Rows.Count == 0 || result.Rows[0]["AverageRating"] == DBNull.Value
                ? 0.0 // Return double
                : Convert.ToDouble(result.Rows[0]["AverageRating"]);
        }

        public async Task<int> GetReviewCountForLocationAsync(int locationId)
        {
            string query = "SELECT COUNT(*) AS ReviewCount FROM Reviews WHERE LocationId = @LocationId";
            SQLiteParameter[] parameters = { new("@LocationId", locationId) };
            // Use injected _sqlClient
            DataTable result = await this._sqlClient.SelectAsync(query, parameters);

            return result.Rows.Count == 0 || result.Rows[0]["ReviewCount"] == DBNull.Value
                 ? 0
                 : Convert.ToInt32(result.Rows[0]["ReviewCount"]);
        }

        public async Task<bool> HasUserReviewedLocationAsync(int userId, int locationId)
        {
            string query = "SELECT 1 FROM Reviews WHERE UserId = @UserId AND LocationId = @LocationId LIMIT 1"; // More efficient query
            SQLiteParameter[] parameters = {
                new("@UserId", userId),
                new("@LocationId", locationId)
            };
            // Use injected _sqlClient
            DataTable result = await this._sqlClient.SelectAsync(query, parameters);

            return result.Rows.Count > 0; // Check if any row was returned
        }

        private ReviewGet MapRowToReviewGet(DataRow row)
        {
            return new ReviewGet
            {
                Id = Convert.ToInt32(row["Id_Reviews"]),
                LocationId = Convert.ToInt32(row["LocationId"]),
                UserId = Convert.ToInt32(row["UserId"]),
                Rating = Convert.ToInt32(row["Rating"]),
                ReviewText = row["ReviewText"].ToString() ?? string.Empty,
                IsAnonymous = Convert.ToBoolean(row["IsAnonymous"]),
                CreatedAt = Convert.ToDateTime(row["CreatedAt"]),
                UpdatedAt = Convert.ToDateTime(row["UpdatedAt"]),
                AuthorName = string.Empty // will be set after mapping
            };
        }
    }
}