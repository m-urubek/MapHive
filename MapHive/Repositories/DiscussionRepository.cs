namespace MapHive.Repositories
{
    using System.Data;
    using System.Data.SQLite;
    using MapHive.Models.RepositoryModels;
    using MapHive.Services;
    using MapHive.Singletons;
    using MapHive.Utilities.Extensions;

    public class DiscussionRepository(
        ISqlClientSingleton sqlClientSingleton,
        IAccountsRepository userRepository,
        ILogManagerService logManagerService) : IDiscussionRepository
    {
        private readonly ISqlClientSingleton _sqlClientSingleton = sqlClientSingleton;
        private readonly IAccountsRepository _userRepository = userRepository;
        private readonly ILogManagerService _logManagerService = logManagerService;

        public async Task<IEnumerable<DiscussionThreadGet>> GetDiscussionThreadsByLocationIdAsync(int locationId)
        {
            List<DiscussionThreadGet> list = new();
            string query = "SELECT * FROM DiscussionThreads WHERE LocationId = @LocationId AND IsReviewThread = 0 ORDER BY CreatedAt DESC";
            SQLiteParameter[] parameters = [new("@LocationId", locationId)];
            DataTable dt = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);
            foreach (DataRow row in dt.Rows)
            {
                DiscussionThreadGet thread = MapRowToThreadGet(row: row);
                thread.AuthorUsername = thread.IsAnonymous ? "Anonymous" : await _userRepository.GetUsernameByIdAsync(accountId: thread.AccountId);
                thread.Messages = [.. await GetMessagesByThreadIdAsync(threadId: thread.Id)];
                list.Add(item: thread);
            }
            return list;
        }

        public async Task<List<DiscussionThreadGet>?> GetAllDiscussionThreadsByLocationIdAsync(int locationId)
        {
            List<DiscussionThreadGet> list = new();
            string query = "SELECT * FROM DiscussionThreads WHERE LocationId = @LocationId ORDER BY CreatedAt DESC";
            SQLiteParameter[] parameters = [new("@LocationId", locationId)];
            DataTable dt = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);
            foreach (DataRow row in dt.Rows)
            {
                DiscussionThreadGet thread = MapRowToThreadGet(row: row);
                thread.AuthorUsername = await _userRepository.GetUsernameByIdAsync(accountId: thread.AccountId);
                thread.Messages = [.. await GetMessagesByThreadIdAsync(threadId: thread.Id)];
                list.Add(item: thread);
            }
            return list.Count > 0 ? list : null;
        }

        public async Task<DiscussionThreadGet?> GetThreadByIdAsync(int id)
        {
            string query = "SELECT * FROM DiscussionThreads WHERE Id_DiscussionThreads = @Id_Log";
            SQLiteParameter[] parameters = [new("@Id_Log", id)];
            DataTable dt = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);
            if (dt.Rows.Count == 0)
            {
                return null;
            }

            DiscussionThreadGet thread = MapRowToThreadGet(row: dt.Rows[0]);
            thread.AuthorUsername = await _userRepository.GetUsernameByIdAsync(accountId: thread.AccountId);
            thread.Messages = [.. await GetMessagesByThreadIdAsync(threadId: thread.Id)];
            return thread;
        }

        public async Task<DiscussionThreadGet> CreateDiscussionThreadAsync(DiscussionThreadCreate dto, string initialMessage)
        {
            DateTime now = DateTime.UtcNow;
            string threadQuery = "INSERT INTO DiscussionThreads (LocationId, AccountId, ThreadName, IsReviewThread, ReviewId, CreatedAt) VALUES (@LocationId, @AccountId, @ThreadName, @IsReviewThread, @ReviewId, @CreatedAt);";
            SQLiteParameter[] threadParams =
            [
                new("@LocationId", dto.LocationId),
                new("@AccountId", dto.AccountId),
                new("@ThreadName", dto.ThreadName),
                new("@IsReviewThread", dto.IsReviewThread),
                new("@ReviewId", dto.ReviewId),
                new("@CreatedAt", now)
            ];
            int threadId = await _sqlClientSingleton.InsertAsync(query: threadQuery, parameters: threadParams);
            string msgQuery = "INSERT INTO ThreadMessages (ThreadId, AccountId, MessageText, IsInitialMessage, IsDeleted, CreatedAt) VALUES (@ThreadId, @AccountId, @MessageText, @IsInitialMessage, @IsDeleted, @CreatedAt);";
            SQLiteParameter[] msgParams =
            [
                new("@ThreadId", threadId),
                new("@AccountId", dto.AccountId),
                new("@MessageText", initialMessage),
                new("@IsInitialMessage", true),
                new("@IsDeleted", false),
                new("@CreatedAt", now)
            ];
            _ = await _sqlClientSingleton.InsertAsync(query: msgQuery, parameters: msgParams);
            DiscussionThreadGet? thread = await GetThreadByIdAsync(id: threadId);
            return thread!;
        }

        public async Task<DiscussionThreadGet> CreateReviewThreadAsync(ReviewThreadCreate dto)
        {
            return await CreateDiscussionThreadAsync(dto: new DiscussionThreadCreate
            {
                LocationId = dto.LocationId,
                AccountId = dto.AccountId,
                ThreadName = dto.ReviewTitle,
                IsReviewThread = true,
                ReviewId = dto.ReviewId,
                CreatedAt = DateTime.UtcNow,
                IsAnonymous = dto.IsAnonymous
            }, initialMessage: string.Empty);
        }

        public async Task<bool> DeleteThreadAsync(int id)
        {
            SQLiteParameter[] parameters = [new("@Id_Log", id)];
            int rows = await _sqlClientSingleton.DeleteAsync(query: "DELETE FROM DiscussionThreads WHERE Id_DiscussionThreads = @Id_Log", parameters: parameters);
            return rows > 0;
        }

        public async Task<List<DiscussionThreadGet>> GetThreadsByAccountIdAsync(int accountId)
        {
            List<DiscussionThreadGet> list = new();
            string query = "SELECT DISTINCT dt.* FROM DiscussionThreads dt LEFT JOIN ThreadMessages tm ON dt.Id_DiscussionThreads = tm.ThreadId WHERE dt.AccountId = @AccountId OR tm.AccountId = @AccountId ORDER BY dt.CreatedAt DESC";
            SQLiteParameter[] parameters = [new("@AccountId", accountId)];
            DataTable dt = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);
            foreach (DataRow row in dt.Rows)
            {
                DiscussionThreadGet thread = MapRowToThreadGet(row: row);
                thread.AuthorUsername = await _userRepository.GetUsernameByIdAsync(accountId: thread.AccountId); //todo use sql join
                thread.Messages = [.. await GetMessagesByThreadIdAsync(threadId: thread.Id)];
                list.Add(item: thread);
            }
            return list;
        }

        public async Task<List<ThreadMessageGet>> GetMessagesByThreadIdAsync(int threadId)
        {
            List<ThreadMessageGet> list = new();
            string query = "SELECT * FROM ThreadMessages WHERE ThreadId = @ThreadId ORDER BY CreatedAt";
            SQLiteParameter[] parameters = [new("@ThreadId", threadId)];
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
            SQLiteParameter[] parameters = [new("@Id_Log", id)];
            DataTable dt = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);
            return dt.Rows.Count == 0 ? null : MapRowToMessageGet(row: dt.Rows[0]);
        }

        public async Task<ThreadMessageGet> AddMessageAsync(ThreadMessageCreate dto)
        {
            DateTime now = DateTime.UtcNow;
            string query = "INSERT INTO ThreadMessages (ThreadId, AccountId, MessageText, IsInitialMessage, IsDeleted, CreatedAt) VALUES (@ThreadId, @AccountId, @MessageText, @IsInitialMessage, @IsDeleted, @CreatedAt); SELECT last_insert_rowid();";
            SQLiteParameter[] parameters =
            [
                new("@ThreadId", dto.ThreadId),
                new("@AccountId", dto.AccountId),
                new("@MessageText", dto.MessageText),
                new("@IsInitialMessage", dto.IsInitialMessage),
                new("@IsDeleted", false),
                new("@CreatedAt", now)
            ];
            int msgId = await _sqlClientSingleton.InsertAsync(query: query, parameters: parameters);
            ThreadMessageGet message = MapRowToMessageGet(row: (await _sqlClientSingleton.SelectAsync(query: "SELECT * FROM ThreadMessages WHERE Id_ThreadMessages = @Id_Log", parameters: [new("@Id_Log", msgId)])).Rows[0]);
            return message;
        }

        public async Task<bool> DeleteMessageAsync(int id, int deletedByAccountId)
        {
            int rows = await _sqlClientSingleton.UpdateAsync(query: "UPDATE ThreadMessages SET IsDeleted=1, DeletedByAccountId=@Del, DeletedAt=@DeletedAt WHERE Id_ThreadMessages=@Id_Log",
parameters: [new("@Del", deletedByAccountId), new("@DeletedAt", DateTime.UtcNow), new("@Id_Log", id)]);
            return rows > 0;
        }

        public async Task<bool> ConvertReviewThreadToDiscussionAsync(int threadId, string initialMessage)
        {
            string query = "UPDATE DiscussionThreads SET IsReviewThread=0 WHERE Id_DiscussionThreads=@Id_Log";
            _ = await _sqlClientSingleton.UpdateAsync(query: query, parameters: [new("@Id_Log", threadId)]);
            return true;
        }

        private DiscussionThreadGet MapRowToThreadGet(DataRow row)
        {
            return new DiscussionThreadGet
            {
                Id = row.GetValueOrThrow<int>(columnName: "Id_DiscussionThreads"),
                LocationId = row.GetValueOrThrow<int>(columnName: "LocationId"),
                AccountId = row.GetValueOrThrow<int>(columnName: "AccountId"),
                ThreadName = row.GetValueOrThrow<string>(columnName: "ThreadName"),
                IsReviewThread = row.GetValueOrThrow<bool>(columnName: "IsReviewThread"),
                CreatedAt = row.GetValueOrThrow<DateTime>(columnName: "CreatedAt"),
                ReviewId = row.GetValueNullable<int>(columnName: "ReviewId"),
                AuthorUsername = string.Empty,
                Messages = new List<ThreadMessageGet>(),
                IsAnonymous = row.GetValueOrThrow<bool>(columnName: "IsAnonymous")
            };
        }

        private ThreadMessageGet MapRowToMessageGet(DataRow row)
        {
            return new ThreadMessageGet
            {
                Id = row.GetValueOrThrow<int>(columnName: "Id_ThreadMessages"),
                ThreadId = row.GetValueOrThrow<int>(columnName: "ThreadId"),
                AccountId = row.GetValueOrThrow<int>(columnName: "AccountId"),
                MessageText = row.GetValueOrThrow<string>(columnName: "MessageText"),
                IsInitialMessage = row.GetValueOrThrow<bool>(columnName: "IsInitialMessage"),
                IsDeleted = row.GetValueOrThrow<bool>(columnName: "IsDeleted"),
                CreatedAt = row.GetValueOrThrow<DateTime>(columnName: "CreatedAt"),
                DeletedAt = row.GetValueNullable<DateTime>(columnName: "DeletedAt"),
                AuthorUsername = string.Empty, // populated by caller
                DeletedByUsername = null
            };
        }
    }
}
