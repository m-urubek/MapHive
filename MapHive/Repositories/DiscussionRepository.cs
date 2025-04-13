using MapHive.Models;
using MapHive.Singletons;
using System.Data;
using System.Data.SQLite;

namespace MapHive.Repositories
{
    public class DiscussionRepository : IDiscussionRepository
    {
        public async Task<IEnumerable<DiscussionThread>> GetDiscussionThreadsByLocationIdAsync(int locationId)
        {
            string query = @"
                SELECT * FROM DiscussionThreads 
                WHERE LocationId = @LocationId AND IsReviewThread = 0
                ORDER BY CreatedAt DESC";

            SQLiteParameter[] parameters = { new("@LocationId", locationId) };
            DataTable result = await CurrentRequest.SqlClient.SelectAsync(query, parameters);

            List<DiscussionThread> threads = new();
            foreach (DataRow row in result.Rows)
            {
                DiscussionThread thread = this.MapRowToThread(row);

                // Get author name
                int userId = Convert.ToInt32(row["UserId"]);
                string username = await CurrentRequest.UserRepository.GetUsernameByIdAsync(userId);
                thread.AuthorName = username;

                // Get initial message
                thread.Messages = (await this.GetMessagesByThreadIdAsync(thread.Id)).ToList();

                threads.Add(thread);
            }

            return threads;
        }

        public async Task<IEnumerable<DiscussionThread>> GetAllDiscussionThreadsByLocationIdAsync(int locationId)
        {
            string query = @"
                SELECT * FROM DiscussionThreads 
                WHERE LocationId = @LocationId
                ORDER BY CreatedAt DESC";

            SQLiteParameter[] parameters = { new("@LocationId", locationId) };
            DataTable result = await CurrentRequest.SqlClient.SelectAsync(query, parameters);

            List<DiscussionThread> threads = new();
            foreach (DataRow row in result.Rows)
            {
                DiscussionThread thread = this.MapRowToThread(row);

                // Get author name
                int userId = Convert.ToInt32(row["UserId"]);
                string username = await CurrentRequest.UserRepository.GetUsernameByIdAsync(userId);
                thread.AuthorName = username;

                // Get messages
                thread.Messages = (await this.GetMessagesByThreadIdAsync(thread.Id)).ToList();

                threads.Add(thread);
            }

            return threads;
        }

        public async Task<DiscussionThread?> GetThreadByIdAsync(int id)
        {
            string query = "SELECT * FROM DiscussionThreads WHERE Id_DiscussionThreads = @Id";
            SQLiteParameter[] parameters = { new("@Id", id) };
            DataTable result = await CurrentRequest.SqlClient.SelectAsync(query, parameters);

            if (result.Rows.Count == 0)
            {
                return null;
            }

            DiscussionThread thread = this.MapRowToThread(result.Rows[0]);

            // Get author name
            int userId = Convert.ToInt32(result.Rows[0]["UserId"]);
            string username = await CurrentRequest.UserRepository.GetUsernameByIdAsync(userId);
            thread.AuthorName = username;

            // Get messages
            thread.Messages = (await this.GetMessagesByThreadIdAsync(thread.Id)).ToList();

            return thread;
        }

        public async Task<DiscussionThread> CreateDiscussionThreadAsync(DiscussionThread thread, string initialMessage)
        {
            try
            {
                // Insert the thread
                string threadQuery = @"
                    INSERT INTO DiscussionThreads (LocationId, UserId, ThreadName, IsReviewThread, ReviewId, CreatedAt)
                    VALUES (@LocationId, @UserId, @ThreadName, @IsReviewThread, @ReviewId, @CreatedAt)";

                SQLiteParameter[] threadParameters = new SQLiteParameter[]
                {
                    new("@LocationId", thread.LocationId),
                    new("@UserId", thread.UserId),
                    new("@ThreadName", thread.ThreadName),
                    new("@IsReviewThread", thread.IsReviewThread),
                    new("@ReviewId", thread.ReviewId as object ?? DBNull.Value),
                    new("@CreatedAt", thread.CreatedAt)
                };

                int threadId = await CurrentRequest.SqlClient.InsertAsync(threadQuery, threadParameters);
                thread.Id = threadId;

                // Insert the initial message
                string messageQuery = @"
                    INSERT INTO ThreadMessages (ThreadId, UserId, MessageText, IsInitialMessage, IsDeleted, CreatedAt)
                    VALUES (@ThreadId, @UserId, @MessageText, @IsInitialMessage, @IsDeleted, @CreatedAt)";

                SQLiteParameter[] messageParameters = new SQLiteParameter[]
                {
                    new("@ThreadId", threadId),
                    new("@UserId", thread.UserId),
                    new("@MessageText", initialMessage),
                    new("@IsInitialMessage", true),
                    new("@IsDeleted", false),
                    new("@CreatedAt", DateTime.UtcNow)
                };

                int messageId = await CurrentRequest.SqlClient.InsertAsync(messageQuery, messageParameters);

                // Load messages
                thread.Messages = (await this.GetMessagesByThreadIdAsync(threadId)).ToList();

                // Get author name
                string username = await CurrentRequest.UserRepository.GetUsernameByIdAsync(thread.UserId);
                thread.AuthorName = username;

                return thread;
            }
            catch
            {
                throw;
            }
        }

        public async Task<DiscussionThread> CreateReviewThreadAsync(int reviewId, string reviewTitle, int locationId)
        {
            DiscussionThread thread = new()
            {
                LocationId = locationId,
                UserId = CurrentRequest.UserId ?? throw new Exception("User ID is not set"),
                ThreadName = $"Discussion for {CurrentRequest.Username}'s review of {reviewId} {reviewTitle}",
                IsReviewThread = true,
                ReviewId = reviewId,
                CreatedAt = DateTime.UtcNow
            };

            string query = @"
                INSERT INTO DiscussionThreads (LocationId, UserId, ThreadName, IsReviewThread, ReviewId, CreatedAt)
                VALUES (@LocationId, @UserId, @ThreadName, @IsReviewThread, @ReviewId, @CreatedAt);
                SELECT last_insert_rowid();";

            SQLiteParameter[] parameters = {
                new("@LocationId", thread.LocationId),
                new("@UserId", thread.UserId),
                new("@ThreadName", thread.ThreadName),
                new("@IsReviewThread", thread.IsReviewThread),
                new("@ReviewId", thread.ReviewId),
                new("@CreatedAt", thread.CreatedAt)
            };

            int threadId = await CurrentRequest.SqlClient.InsertAsync(query, parameters);
            thread.Id = threadId;

            // Get author name
            string username = await CurrentRequest.UserRepository.GetUsernameByIdAsync(thread.UserId);
            thread.AuthorName = username;

            return thread;
        }

        public async Task<bool> DeleteThreadAsync(int id)
        {
            string query = "DELETE FROM DiscussionThreads WHERE Id_DiscussionThreads = @Id";
            SQLiteParameter[] parameters = { new("@Id", id) };

            int rowsAffected = await CurrentRequest.SqlClient.DeleteAsync(query, parameters);
            return rowsAffected > 0;
        }

        public async Task<IEnumerable<DiscussionThread>> GetThreadsByUserIdAsync(int userId)
        {
            // Get threads created by the user or where the user has posted messages
            string query = @"
                SELECT DISTINCT dt.* FROM DiscussionThreads dt
                LEFT JOIN ThreadMessages tm ON dt.Id_DiscussionThreads = tm.ThreadId
                WHERE dt.UserId = @UserId OR tm.UserId = @UserId
                ORDER BY dt.CreatedAt DESC";

            SQLiteParameter[] parameters = { new("@UserId", userId) };
            DataTable result = await CurrentRequest.SqlClient.SelectAsync(query, parameters);

            List<DiscussionThread> threads = new();
            foreach (DataRow row in result.Rows)
            {
                DiscussionThread thread = this.MapRowToThread(row);

                // Get author name
                int authorId = Convert.ToInt32(row["UserId"]);
                string username = await CurrentRequest.UserRepository.GetUsernameByIdAsync(authorId);
                thread.AuthorName = username;

                // Get messages
                thread.Messages = (await this.GetMessagesByThreadIdAsync(thread.Id)).ToList();

                threads.Add(thread);
            }

            return threads;
        }

        public async Task<IEnumerable<ThreadMessage>> GetMessagesByThreadIdAsync(int threadId)
        {
            string query = @"
                SELECT * FROM ThreadMessages 
                WHERE ThreadId = @ThreadId
                ORDER BY CreatedAt ASC";

            SQLiteParameter[] parameters = { new("@ThreadId", threadId) };
            DataTable result = await CurrentRequest.SqlClient.SelectAsync(query, parameters);

            List<ThreadMessage> messages = new();
            foreach (DataRow row in result.Rows)
            {
                ThreadMessage message = this.MapRowToMessage(row);

                // Get author name
                int userId = Convert.ToInt32(row["UserId"]);
                string username = await CurrentRequest.UserRepository.GetUsernameByIdAsync(userId);
                message.AuthorName = username;

                // Get deleted by username if applicable
                if (message.IsDeleted && message.DeletedByUserId.HasValue)
                {
                    message.DeletedByUsername = await CurrentRequest.UserRepository.GetUsernameByIdAsync(message.DeletedByUserId.Value);
                }

                messages.Add(message);
            }

            return messages;
        }

        public async Task<ThreadMessage?> GetMessageByIdAsync(int id)
        {
            string query = "SELECT * FROM ThreadMessages WHERE Id_ThreadMessages = @Id";
            SQLiteParameter[] parameters = { new("@Id", id) };
            DataTable result = await CurrentRequest.SqlClient.SelectAsync(query, parameters);

            if (result.Rows.Count == 0)
            {
                return null;
            }

            ThreadMessage message = this.MapRowToMessage(result.Rows[0]);

            // Get author name
            int userId = Convert.ToInt32(result.Rows[0]["UserId"]);
            string username = await CurrentRequest.UserRepository.GetUsernameByIdAsync(userId);
            message.AuthorName = username;

            // Get deleted by username if applicable
            if (message.IsDeleted && message.DeletedByUserId.HasValue)
            {
                message.DeletedByUsername = await CurrentRequest.UserRepository.GetUsernameByIdAsync(message.DeletedByUserId.Value);
            }

            return message;
        }

        public async Task<ThreadMessage> AddMessageAsync(ThreadMessage message)
        {
            string query = @"
                INSERT INTO ThreadMessages (ThreadId, UserId, MessageText, IsInitialMessage, IsDeleted, CreatedAt)
                VALUES (@ThreadId, @UserId, @MessageText, @IsInitialMessage, @IsDeleted, @CreatedAt);
                SELECT last_insert_rowid();";

            SQLiteParameter[] parameters = {
                new("@ThreadId", message.ThreadId),
                new("@UserId", message.UserId),
                new("@MessageText", message.MessageText),
                new("@IsInitialMessage", message.IsInitialMessage),
                new("@IsDeleted", message.IsDeleted),
                new("@CreatedAt", message.CreatedAt)
            };

            int messageId = await CurrentRequest.SqlClient.InsertAsync(query, parameters);
            message.Id = messageId;

            // Get author name
            string username = await CurrentRequest.UserRepository.GetUsernameByIdAsync(message.UserId);
            message.AuthorName = username;

            return message;
        }

        public async Task<bool> DeleteMessageAsync(int id, int deletedByUserId)
        {
            string query = @"
                UPDATE ThreadMessages 
                SET IsDeleted = 1, DeletedByUserId = @DeletedByUserId
                WHERE Id_ThreadMessages = @Id";

            SQLiteParameter[] parameters = {
                new("@Id", id),
                new("@DeletedByUserId", deletedByUserId)
            };

            int rowsAffected = await CurrentRequest.SqlClient.UpdateAsync(query, parameters);
            return rowsAffected > 0;
        }

        public async Task<bool> ConvertReviewThreadToDiscussionAsync(int threadId, string initialMessage)
        {
            string query = @"
                UPDATE DiscussionThreads
                SET IsReviewThread = 0
                WHERE Id_DiscussionThreads = @Id";

            SQLiteParameter[] parameters = { new("@Id", threadId) };

            int rowsAffected = await CurrentRequest.SqlClient.UpdateAsync(query, parameters);

            if (rowsAffected > 0)
            {
                // Get the thread
                DiscussionThread? thread = await this.GetThreadByIdAsync(threadId);
                if (thread == null)
                {
                    return false;
                }

                // Add initial message
                ThreadMessage message = new()
                {
                    ThreadId = threadId,
                    UserId = thread.UserId,
                    MessageText = initialMessage,
                    IsInitialMessage = true,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow
                };

                _ = await this.AddMessageAsync(message);
                return true;
            }

            return false;
        }

        private DiscussionThread MapRowToThread(DataRow row)
        {
            return new DiscussionThread
            {
                Id = Convert.ToInt32(row["Id_DiscussionThreads"]),
                LocationId = Convert.ToInt32(row["LocationId"]),
                UserId = Convert.ToInt32(row["UserId"]),
                ThreadName = row["ThreadName"].ToString() ?? string.Empty,
                IsReviewThread = Convert.ToBoolean(row["IsReviewThread"]),
                ReviewId = row["ReviewId"] == DBNull.Value ? null : Convert.ToInt32(row["ReviewId"]),
                CreatedAt = Convert.ToDateTime(row["CreatedAt"])
            };
        }

        private ThreadMessage MapRowToMessage(DataRow row)
        {
            return new ThreadMessage
            {
                Id = Convert.ToInt32(row["Id_ThreadMessages"]),
                ThreadId = Convert.ToInt32(row["ThreadId"]),
                UserId = Convert.ToInt32(row["UserId"]),
                MessageText = row["MessageText"].ToString() ?? string.Empty,
                IsInitialMessage = Convert.ToBoolean(row["IsInitialMessage"]),
                IsDeleted = Convert.ToBoolean(row["IsDeleted"]),
                DeletedByUserId = row["DeletedByUserId"] == DBNull.Value ? null : Convert.ToInt32(row["DeletedByUserId"]),
                CreatedAt = Convert.ToDateTime(row["CreatedAt"])
            };
        }
    }
}