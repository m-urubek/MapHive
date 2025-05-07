using MapHive.Models.RepositoryModels;
using MapHive.Singletons;
using System.Data;
using System.Data.SQLite;

namespace MapHive.Repositories
{
    public class DiscussionRepository : IDiscussionRepository
    {
        private readonly ISqlClientSingleton _sqlClient;
        private readonly IUserRepository _userRepository;

        public DiscussionRepository(ISqlClientSingleton sqlClient, IUserRepository userRepository)
        {
            this._sqlClient = sqlClient;
            this._userRepository = userRepository;
        }

        public async Task<IEnumerable<DiscussionThreadGet>> GetDiscussionThreadsByLocationIdAsync(int locationId)
        {
            List<DiscussionThreadGet> list = new();
            string query = @"SELECT * FROM DiscussionThreads WHERE LocationId = @LocationId AND IsReviewThread = 0 ORDER BY CreatedAt DESC";
            SQLiteParameter[] parameters = new SQLiteParameter[] { new("@LocationId", locationId) };
            DataTable dt = await this._sqlClient.SelectAsync(query, parameters);
            foreach (DataRow row in dt.Rows)
            {
                DiscussionThreadGet thread = this.MapRowToThreadGet(row);
                string username = await this._userRepository.GetUsernameByIdAsync(thread.UserId);
                thread.AuthorName = username;
                thread.Messages = (await this.GetMessagesByThreadIdAsync(thread.Id)).ToList();
                list.Add(thread);
            }
            return list;
        }

        public async Task<IEnumerable<DiscussionThreadGet>> GetAllDiscussionThreadsByLocationIdAsync(int locationId)
        {
            List<DiscussionThreadGet> list = new();
            string query = @"SELECT * FROM DiscussionThreads WHERE LocationId = @LocationId ORDER BY CreatedAt DESC";
            SQLiteParameter[] parameters = new SQLiteParameter[] { new("@LocationId", locationId) };
            DataTable dt = await this._sqlClient.SelectAsync(query, parameters);
            foreach (DataRow row in dt.Rows)
            {
                DiscussionThreadGet thread = this.MapRowToThreadGet(row);
                string username = await this._userRepository.GetUsernameByIdAsync(thread.UserId);
                thread.AuthorName = username;
                thread.Messages = (await this.GetMessagesByThreadIdAsync(thread.Id)).ToList();
                list.Add(thread);
            }
            return list;
        }

        public async Task<DiscussionThreadGet?> GetThreadByIdAsync(int id)
        {
            string query = "SELECT * FROM DiscussionThreads WHERE Id_DiscussionThreads = @Id_Log";
            SQLiteParameter[] parameters = new SQLiteParameter[] { new("@Id_Log", id) };
            DataTable dt = await this._sqlClient.SelectAsync(query, parameters);
            if (dt.Rows.Count == 0)
            {
                return null;
            }

            DiscussionThreadGet thread = this.MapRowToThreadGet(dt.Rows[0]);
            string username = await this._userRepository.GetUsernameByIdAsync(thread.UserId);
            thread.AuthorName = username;
            thread.Messages = (await this.GetMessagesByThreadIdAsync(thread.Id)).ToList();
            return thread;
        }

        public async Task<DiscussionThreadGet> CreateDiscussionThreadAsync(DiscussionThreadCreate dto, string initialMessage)
        {
            DateTime now = DateTime.UtcNow;
            string threadQuery = @"INSERT INTO DiscussionThreads (LocationId, UserId, ThreadName, IsReviewThread, ReviewId, CreatedAt) VALUES (@LocationId, @UserId, @ThreadName, @IsReviewThread, @ReviewId, @CreatedAt);";
            SQLiteParameter[] threadParams = new SQLiteParameter[]
            {
                new("@LocationId", dto.LocationId),
                new("@UserId", dto.UserId),
                new("@ThreadName", dto.ThreadName),
                new("@IsReviewThread", dto.IsReviewThread),
                new("@ReviewId", dto.ReviewId.HasValue ? (object)dto.ReviewId.Value : DBNull.Value),
                new("@CreatedAt", now)
            };
            int threadId = await this._sqlClient.InsertAsync(threadQuery, threadParams);
            string msgQuery = @"INSERT INTO ThreadMessages (ThreadId, UserId, MessageText, IsInitialMessage, IsDeleted, CreatedAt) VALUES (@ThreadId, @UserId, @MessageText, @IsInitialMessage, @IsDeleted, @CreatedAt);";
            SQLiteParameter[] msgParams = new SQLiteParameter[]
            {
                new("@ThreadId", threadId),
                new("@UserId", dto.UserId),
                new("@MessageText", initialMessage),
                new("@IsInitialMessage", true),
                new("@IsDeleted", false),
                new("@CreatedAt", now)
            };
            _ = await this._sqlClient.InsertAsync(msgQuery, msgParams);
            DiscussionThreadGet? thread = await this.GetThreadByIdAsync(threadId);
            return thread!;
        }

        public async Task<DiscussionThreadGet> CreateReviewThreadAsync(ReviewThreadCreate dto)
        {
            // Prepared via interface edit
            return await this.CreateDiscussionThreadAsync(new DiscussionThreadCreate
            {
                LocationId = dto.LocationId,
                UserId = dto.UserId,
                ThreadName = dto.ReviewTitle,
                IsReviewThread = true,
                ReviewId = dto.ReviewId
            }, string.Empty);
        }

        public async Task<bool> DeleteThreadAsync(int id)
        {
            SQLiteParameter[] parameters = new SQLiteParameter[] { new("@Id_Log", id) };
            int rows = await this._sqlClient.DeleteAsync("DELETE FROM DiscussionThreads WHERE Id_DiscussionThreads = @Id_Log", parameters);
            return rows > 0;
        }

        public async Task<IEnumerable<DiscussionThreadGet>> GetThreadsByUserIdAsync(int userId)
        {
            List<DiscussionThreadGet> list = new();
            string query = @"SELECT DISTINCT dt.* FROM DiscussionThreads dt LEFT JOIN ThreadMessages tm ON dt.Id_DiscussionThreads = tm.ThreadId WHERE dt.UserId = @UserId OR tm.UserId = @UserId ORDER BY dt.CreatedAt DESC";
            SQLiteParameter[] parameters = new SQLiteParameter[] { new("@UserId", userId) };
            DataTable dt = await this._sqlClient.SelectAsync(query, parameters);
            foreach (DataRow row in dt.Rows)
            {
                DiscussionThreadGet thread = this.MapRowToThreadGet(row);
                thread.AuthorName = await this._userRepository.GetUsernameByIdAsync(thread.UserId);
                thread.Messages = (await this.GetMessagesByThreadIdAsync(thread.Id)).ToList();
                list.Add(thread);
            }
            return list;
        }

        public async Task<IEnumerable<ThreadMessageGet>> GetMessagesByThreadIdAsync(int threadId)
        {
            List<ThreadMessageGet> list = new();
            string query = "SELECT * FROM ThreadMessages WHERE ThreadId = @ThreadId ORDER BY CreatedAt";
            SQLiteParameter[] parameters = new SQLiteParameter[] { new("@ThreadId", threadId) };
            DataTable dt = await this._sqlClient.SelectAsync(query, parameters);
            foreach (DataRow row in dt.Rows)
            {
                list.Add(this.MapRowToMessageGet(row));
            }

            return list;
        }

        public async Task<ThreadMessageGet?> GetMessageByIdAsync(int id)
        {
            string query = "SELECT * FROM ThreadMessages WHERE Id_ThreadMessages = @Id_Log";
            SQLiteParameter[] parameters = new SQLiteParameter[] { new("@Id_Log", id) };
            DataTable dt = await this._sqlClient.SelectAsync(query, parameters);
            return dt.Rows.Count == 0 ? null : this.MapRowToMessageGet(dt.Rows[0]);
        }

        public async Task<ThreadMessageGet> AddMessageAsync(ThreadMessageCreate dto)
        {
            DateTime now = DateTime.UtcNow;
            string query = @"INSERT INTO ThreadMessages (ThreadId, UserId, MessageText, IsInitialMessage, IsDeleted, CreatedAt) VALUES (@ThreadId, @UserId, @MessageText, @IsInitialMessage, @IsDeleted, @CreatedAt); SELECT last_insert_rowid();";
            SQLiteParameter[] parameters = new SQLiteParameter[]
            {
                new("@ThreadId", dto.ThreadId),
                new("@UserId", dto.UserId),
                new("@MessageText", dto.MessageText),
                new("@IsInitialMessage", dto.IsInitialMessage),
                new("@IsDeleted", false),
                new("@CreatedAt", now)
            };
            int msgId = await this._sqlClient.InsertAsync(query, parameters);
            ThreadMessageGet message = this.MapRowToMessageGet((await this._sqlClient.SelectAsync("SELECT * FROM ThreadMessages WHERE Id_ThreadMessages = @Id_Log", new SQLiteParameter[] { new("@Id_Log", msgId) })).Rows[0]);
            return message;
        }

        public async Task<bool> DeleteMessageAsync(int id, int deletedByUserId)
        {
            int rows = await this._sqlClient.UpdateAsync("UPDATE ThreadMessages SET IsDeleted=1, DeletedByUserId=@Del, DeletedAt=@DeletedAt WHERE Id_ThreadMessages=@Id_Log",
                new SQLiteParameter[] { new("@Del", deletedByUserId), new("@DeletedAt", DateTime.UtcNow), new("@Id_Log", id) });
            return rows > 0;
        }

        public async Task<bool> ConvertReviewThreadToDiscussionAsync(int threadId, string initialMessage)
        {
            string query = "UPDATE DiscussionThreads SET IsReviewThread=0 WHERE Id_DiscussionThreads=@Id_Log";
            _ = await this._sqlClient.UpdateAsync(query, new SQLiteParameter[] { new("@Id_Log", threadId) });
            return true;
        }

        private DiscussionThreadGet MapRowToThreadGet(DataRow row)
        {
            return new DiscussionThreadGet
            {
                Id = Convert.ToInt32(row["Id_DiscussionThreads"]),
                LocationId = Convert.ToInt32(row["LocationId"]),
                UserId = Convert.ToInt32(row["UserId"]),
                ThreadName = row["ThreadName"].ToString() ?? string.Empty,
                IsReviewThread = Convert.ToBoolean(row["IsReviewThread"]),
                ReviewId = row["ReviewId"] != DBNull.Value ? Convert.ToInt32(row["ReviewId"]) : null,
                CreatedAt = Convert.ToDateTime(row["CreatedAt"]),
                Messages = new List<ThreadMessageGet>() // will be populated
            };
        }

        private ThreadMessageGet MapRowToMessageGet(DataRow row)
        {
            return new ThreadMessageGet
            {
                Id = Convert.ToInt32(row["Id_ThreadMessages"]),
                ThreadId = Convert.ToInt32(row["ThreadId"]),
                UserId = Convert.ToInt32(row["UserId"]),
                MessageText = row["MessageText"].ToString() ?? string.Empty,
                IsInitialMessage = Convert.ToBoolean(row["IsInitialMessage"]),
                IsDeleted = Convert.ToBoolean(row["IsDeleted"]),
                DeletedByUserId = row["DeletedByUserId"] != DBNull.Value ? Convert.ToInt32(row["DeletedByUserId"]) : (int?)null,
                CreatedAt = Convert.ToDateTime(row["CreatedAt"]),
                DeletedAt = row["DeletedAt"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(row["DeletedAt"]) : null,
                AuthorName = string.Empty, // populated by caller
                DeletedByUsername = null
            };
        }
    }
}