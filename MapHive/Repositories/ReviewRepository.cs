namespace MapHive.Repositories
{
    using System.Data;
    using System.Data.SQLite;
    using MapHive.Models.RepositoryModels;
    using MapHive.Services;
    using MapHive.Singletons;
    using MapHive.Utilities.Extensions;

    public class ReviewRepository(ISqlClientSingleton sqlClientSingleton, IAccountsRepository userRepository, ILogManagerService logManagerService) : IReviewRepository
    {
        private readonly ISqlClientSingleton _sqlClientSingleton = sqlClientSingleton;
        private readonly IAccountsRepository _userRepository = userRepository;
        private readonly ILogManagerService _logManagerService = logManagerService;

        public async Task<List<ReviewGet>?> GetReviewsByLocationIdAsync(int locationId)
        {
            string query = @"
                SELECT r.*, u.Username
                FROM Reviews r
                LEFT JOIN Users u ON r.AccountId = u.Id_Account
                WHERE r.LocationId = @LocationId
                ORDER BY r.CreatedAt DESC";

            SQLiteParameter[] parameters = [new("@LocationId", locationId)];
            DataTable result = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);

            List<ReviewGet> reviews = new();
            foreach (DataRow row in result.Rows)
            {
                ReviewGet rg = MapRowToReviewGet(row: row);
                string? username = row.Table.Columns.Contains(name: "Username") && row["Username"] != DBNull.Value
                                     ? row["Username"].ToString()
                                     : await _userRepository.GetUsernameByIdAsync(accountId: rg.AccountId);
                rg.AuthorUsername = rg.IsAnonymous ? "Anonymous" : (username ?? "Unknown UserLogin");
                reviews.Add(item: rg);
            }

            return reviews;
        }

        public async Task<ReviewGet?> GetReviewByIdAsync(int id)
        {
            string query = "SELECT * FROM Reviews WHERE Id_Reviews = @Id_Log";
            SQLiteParameter[] parameters = [new("@Id_Log", id)];
            DataTable result = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);

            if (result.Rows.Count == 0)
            {
                return null;
            }

            ReviewGet rg = MapRowToReviewGet(row: result.Rows[0]);
            string username = await _userRepository.GetUsernameByIdAsync(accountId: rg.AccountId);
            rg.AuthorUsername = rg.IsAnonymous ? "Anonymous" : (username ?? "Unknown UserLogin");

            return rg;
        }

        public async Task<ReviewGet> AddReviewAsync(ReviewCreate review)
        {
            DateTime now = DateTime.UtcNow;
            string query = @"
                INSERT INTO Reviews (LocationId, AccountId, Rating, ReviewText, IsAnonymous, CreatedAt, UpdatedAt)
                VALUES (@LocationId, @AccountId, @Rating, @ReviewText, @IsAnonymous, @CreatedAt, @UpdatedAt);";

            SQLiteParameter[] parameters = [
                new("@LocationId", review.LocationId),
                new("@AccountId", review.AccountId),
                new("@Rating", review.Rating),
                new("@ReviewText", review.ReviewText),
                new("@IsAnonymous", review.IsAnonymous),
                new("@CreatedAt", now),
                new("@UpdatedAt", now)
            ];

            int reviewId = await _sqlClientSingleton.InsertAsync(query: query, parameters: parameters);
            string username = await _userRepository.GetUsernameByIdAsync(accountId: review.AccountId);

            return new ReviewGet
            {
                Id = reviewId,
                LocationId = review.LocationId,
                AccountId = review.AccountId,
                Rating = review.Rating,
                ReviewText = review.ReviewText,
                IsAnonymous = review.IsAnonymous,
                CreatedAt = now,
                UpdatedAt = now,
                AuthorUsername = review.IsAnonymous ? "Anonymous" : (username ?? "Unknown UserLogin")
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
                WHERE Id_Reviews = @Id_Log AND AccountId = @AccountId";

            SQLiteParameter[] parameters = [
                new("@Id_Log", review.Id),
                new("@Rating", review.Rating),
                new("@ReviewText", review.ReviewText),
                new("@IsAnonymous", review.IsAnonymous),
                new("@UpdatedAt", now),
                new("@AccountId", review.AccountId)
            ];

            int rowsAffected = await _sqlClientSingleton.UpdateAsync(query: query, parameters: parameters);
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteReviewAsync(int id)
        {
            // Consider adding user ID check if only owners can delete
            string query = "DELETE FROM Reviews WHERE Id_Reviews = @Id_Log";
            SQLiteParameter[] parameters = [new("@Id_Log", id)];
            // Use injected _sqlClientSingleton
            int rowsAffected = await _sqlClientSingleton.DeleteAsync(query: query, parameters: parameters);
            return rowsAffected > 0;
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
            string query = "SELECT 1 FROM Reviews WHERE AccountId = @AccountId AND LocationId = @LocationId LIMIT 1"; // More efficient query
            SQLiteParameter[] parameters = [
                new("@AccountId", accountId),
                new("@LocationId", locationId)
            ];
            // Use injected _sqlClientSingleton
            DataTable result = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);

            return result.Rows.Count > 0; // Check if any row was returned
        }

        private ReviewGet MapRowToReviewGet(DataRow row)
        {
            return new ReviewGet
            {
                Id = row.GetValueOrThrow<int>(columnName: "Id_Reviews"),
                LocationId = row.GetValueOrThrow<int>(columnName: "LocationId"),
                AccountId = row.GetValueOrThrow<int>(columnName: "AccountId"),
                Rating = row.GetValueOrThrow<int>(columnName: "Rating"),
                ReviewText = row.GetValueOrThrow<string>(columnName: "ReviewText"),
                IsAnonymous = row.GetValueOrThrow<bool>(columnName: "IsAnonymous"),
                CreatedAt = row.GetValueOrThrow<DateTime>(columnName: "CreatedAt"),
                UpdatedAt = row.GetValueOrThrow<DateTime>(columnName: "UpdatedAt"),
                AuthorUsername = string.Empty // todo use join with users table
            };
        }
    }
}
