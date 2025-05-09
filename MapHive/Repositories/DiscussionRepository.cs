namespace MapHive.Repositories
{
    using System.Data;
    using System.Data.SQLite;
    using MapHive.Models.RepositoryModels;
    using MapHive.Singletons;

    public class DiscussionRepository(ISqlClientSingleton sqlClientSingleton, IUserRepository userRepository) : IDiscussionRepository
    {
        private readonly ISqlClientSingleton _sqlClientSingleton = sqlClientSingleton;
        private readonly IUserRepository _userRepository = userRepository;

        public async Task<IEnumerable<DiscussionThreadGet>> GetDiscussionThreadsByLocationIdAsync(int locationId)
        {
            List<DiscussionThreadGet> list = new();
            string query = @"SELECT * FROM DiscussionThreads WHERE LocationId = @LocationId AND IsReviewThread = 0 ORDER BY CreatedAt DESC";
            SQLiteParameter[] parameters = new SQLiteParameter[] { new("@LocationId", locationId) };
            DataTable dt = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);
            foreach (DataRow row in dt.Rows)
            {
                DiscussionThreadGet thread = MapRowToThreadGet(row: row);
                string username = await _userRepository.GetUsernameByIdAsync(userId: thread.UserId);
                thread.AuthorName = username;
                thread.Messages = (await GetMessagesByThreadIdAsync(threadId: thread.Id)).ToList();
                list.Add(item: thread);
            }
            return list;
        }

        public async Task<IEnumerable<DiscussionThreadGet>> GetAllDiscussionThreadsByLocationIdAsync(int locationId)
        {
            List<DiscussionThreadGet> list = new();
            string query = @"SELECT * FROM DiscussionThreads WHERE LocationId = @LocationId ORDER BY CreatedAt DESC";
            SQLiteParameter[] parameters = new SQLiteParameter[] { new("@LocationId", locationId) };
            DataTable dt = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);
            foreach (DataRow row in dt.Rows)
            {
                DiscussionThreadGet thread = MapRowToThreadGet(row: row);
                string username = await _userRepository.GetUsernameByIdAsync(userId: thread.UserId);
                thread.AuthorName = username;
                thread.Messages = (await GetMessagesByThreadIdAsync(threadId: thread.Id)).ToList();
                list.Add(item: thread);
            }
            return list;
        }

        public async Task<DiscussionThreadGet?> GetThreadByIdAsync(int id)
        {
            string query = "SELECT * FROM DiscussionThreads WHERE Id_DiscussionThreads = @Id_Log";
            SQLiteParameter[] parameters = new SQLiteParameter[] { new("@Id_Log", id) };
            DataTable dt = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);
            if (dt.Rows.Count == 0)
            {
                return null;
            }

            DiscussionThreadGet thread = MapRowToThreadGet(row: dt.Rows[0]);
            string username = await _userRepository.GetUsernameByIdAsync(userId: thread.UserId);
            thread.AuthorName = username;
            thread.Messages = (await GetMessagesByThreadIdAsync(threadId: thread.Id)).ToList();
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
            int threadId = await _sqlClientSingleton.InsertAsync(query: threadQuery, parameters: threadParams);
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
            _ = await _sqlClientSingleton.InsertAsync(query: msgQuery, parameters: msgParams);
            DiscussionThreadGet? thread = await GetThreadByIdAsync(id: threadId);
            return thread!;
        }

        public async Task<DiscussionThreadGet> CreateReviewThreadAsync(ReviewThreadCreate dto)
        {
            // Prepared via interface edit
            return await CreateDiscussionThreadAsync(dto: new DiscussionThreadCreate
            {
                LocationId = dto.LocationId,
                UserId = dto.UserId,
                ThreadName = dto.ReviewTitle,
                IsReviewThread = true,
                ReviewId = dto.ReviewId
            }, initialMessage: string.Empty);
        }

        public async Task<bool> DeleteThreadAsync(int id)
        {
            SQLiteParameter[] parameters = new SQLiteParameter[] { new("@Id_Log", id) };
            int rows = await _sqlClientSingleton.DeleteAsync(query: "DELETE FROM DiscussionThreads WHERE Id_DiscussionThreads = @Id_Log", parameters: parameters);
            return rows > 0;
        }

        public async Task<IEnumerable<DiscussionThreadGet>> GetThreadsByUserIdAsync(int userId)
        {
            List<DiscussionThreadGet> list = new();
            string query = @"SELECT DISTINCT dt.* FROM DiscussionThreads dt LEFT JOIN ThreadMessages tm ON dt.Id_DiscussionThreads = tm.ThreadId WHERE dt.UserId = @UserId OR tm.UserId = @UserId ORDER BY dt.CreatedAt DESC";
            SQLiteParameter[] parameters = new SQLiteParameter[] { new("@UserId", userId) };
            DataTable dt = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);
            foreach (DataRow row in dt.Rows)
            {
                DiscussionThreadGet thread = MapRowToThreadGet(row: row);
                thread.AuthorName = await _userRepository.GetUsernameByIdAsync(userId: thread.UserId);
                thread.Messages = (await GetMessagesByThreadIdAsync(threadId: thread.Id)).ToList();
                list.Add(item: thread);
            }
            return list;
        }

        public async Task<IEnumerable<ThreadMessageGet>> GetMessagesByThreadIdAsync(int threadId)
        {
            List<ThreadMessageGet> list = new();
            string query = "SELECT * FROM ThreadMessages WHERE ThreadId = @ThreadId ORDER BY CreatedAt";
            SQLiteParameter[] parameters = new SQLiteParameter[] { new("@ThreadId", threadId) };
            DataTable dt = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);
            foreach (DataRow row in dt.Rows)
            {
                list.Add(item: MapRowToMessageGet(row: row));
            }

            return list;
        }

        public async Task<ThreadMessageGet?> GetMessageByIdAsync(int id)
        {
            string query = "SELECT * FROM ThreadMessages WHERE Id_ThreadMessages = @Id_Log";
            SQLiteParameter[] parameters = new SQLiteParameter[] { new("@Id_Log", id) };
            DataTable dt = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);
            return dt.Rows.Count == 0 ? null : MapRowToMessageGet(row: dt.Rows[0]);
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
            int msgId = await _sqlClientSingleton.InsertAsync(query: query, parameters: parameters);
            ThreadMessageGet message = MapRowToMessageGet(row: (await _sqlClientSingleton.SelectAsync(query: "SELECT * FROM ThreadMessages WHERE Id_ThreadMessages = @Id_Log", parameters: new SQLiteParameter[] { new("@Id_Log", msgId) })).Rows[0]);
            return message;
        }

        public async Task<bool> DeleteMessageAsync(int id, int deletedByUserId)
        {
            int rows = await _sqlClientSingleton.UpdateAsync(query: "UPDATE ThreadMessages SET IsDeleted=1, DeletedByUserId=@Del, DeletedAt=@DeletedAt WHERE Id_ThreadMessages=@Id_Log",
parameters: new SQLiteParameter[] { new("@Del", deletedByUserId), new("@DeletedAt", DateTime.UtcNow), new("@Id_Log", id) });
            return rows > 0;
        }

        public async Task<bool> ConvertReviewThreadToDiscussionAsync(int threadId, string initialMessage)
        {
            string query = "UPDATE DiscussionThreads SET IsReviewThread=0 WHERE Id_DiscussionThreads=@Id_Log";
            _ = await _sqlClientSingleton.UpdateAsync(query: query, parameters: new SQLiteParameter[] { new("@Id_Log", threadId) });
            return true;
        }

        private static DiscussionThreadGet MapRowToThreadGet(DataRow row)
        {
            return new DiscussionThreadGet
            {
                Id = Convert.ToInt32(value: row["Id_DiscussionThreads"]),
                LocationId = Convert.ToInt32(value: row["LocationId"]),
                UserId = Convert.ToInt32(value: row["UserId"]),
                ThreadName = row["ThreadName"].ToString() ?? string.Empty,
                IsReviewThread = Convert.ToBoolean(value: row["IsReviewThread"]),
                ReviewId = row["ReviewId"] != DBNull.Value ? Convert.ToInt32(value: row["ReviewId"]) : null,
                CreatedAt = Convert.ToDateTime(value: row["CreatedAt"]),
                Messages = new List<ThreadMessageGet>() // will be populated
            };
        }

        private static ThreadMessageGet MapRowToMessageGet(DataRow row)
        {
            return new ThreadMessageGet
            {
                Id = Convert.ToInt32(value: row["Id_ThreadMessages"]),
                ThreadId = Convert.ToInt32(value: row["ThreadId"]),
                UserId = Convert.ToInt32(value: row["UserId"]),
                MessageText = row["MessageText"].ToString() ?? string.Empty,
                IsInitialMessage = Convert.ToBoolean(value: row["IsInitialMessage"]),
                IsDeleted = Convert.ToBoolean(value: row["IsDeleted"]),
                DeletedByUserId = row["DeletedByUserId"] != DBNull.Value ? Convert.ToInt32(value: row["DeletedByUserId"]) : (int?)null,
                CreatedAt = Convert.ToDateTime(value: row["CreatedAt"]),
                DeletedAt = row["DeletedAt"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(value: row["DeletedAt"]) : null,
                AuthorName = string.Empty, // populated by caller
                DeletedByUsername = null
            };
        }
    }
}