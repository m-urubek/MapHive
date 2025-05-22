namespace MapHive.Repositories;

using System.Data;
using System.Data.SQLite;
using MapHive.Models.Data.DbTableModels;
using MapHive.Models.Exceptions;
using MapHive.Models.PageModels;
using MapHive.Singletons;

public class DiscussionRepository(
    ISqlClientSingleton _sqlClientSingleton) : IDiscussionRepository
{
    public async Task<ThreadDisplayPageModel> GetThreadByIdOrThrowAsync(int id)
    {
        return await GetThreadByKeyValueOrThrowAsync(key: "Id_DiscussionThreads", value: id.ToString());
    }

    public async Task<ThreadDisplayPageModel> GetThreadByReviewIdOrThrowAsync(int reviewId)
    {
        return await GetThreadByKeyValueOrThrowAsync(key: "ReviewId", value: reviewId.ToString());
    }

    public async Task<ThreadDisplayPageModel> GetThreadByKeyValueOrThrowAsync(string key, string value)
    {
        string query = $"""
            SELECT dt.*, r.Rating, u.Username, ml.Name
            FROM DiscussionThreads dt
            LEFT JOIN Reviews r ON dt.ReviewId = r.Id_Reviews
            LEFT JOIN Accounts u ON dt.AuthorId = u.Id_Accounts
            LEFT JOIN MapLocations ml ON dt.LocationId = ml.Id_MapLocations
            WHERE dt.{key} = @value
            """;
        SQLiteParameter[] parameters = [new("@value", value)];
        DataTable dt = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);
        return dt.Rows.Count == 0
            ? throw new PublicErrorException($"Thread with '{key}' = '{value}' not found")
            : await MapRowToThreadDisplay(row: dt.Rows[0]);
    }

    public async Task<int> CreateDiscussionThreadAsync(
        int locationId,
        int accountId,
        string threadName,
        int? reviewId,
        bool isAnonymous
    )
    {
        DateTime now = DateTime.UtcNow;
        string threadQuery = "INSERT INTO DiscussionThreads (LocationId, AuthorId, ThreadName, ReviewId, IsAnonymous, CreatedAt) VALUES (@LocationId, @AuthorId, @ThreadName, @ReviewId, @IsAnonymous, @CreatedAt);";
        SQLiteParameter[] threadParams =
        [
            new("@LocationId", locationId),
            new("@AuthorId", accountId),
            new("@ThreadName", threadName),
            new("@ReviewId", reviewId),
            new("@IsAnonymous", isAnonymous),
            new("@CreatedAt", now)
        ];
        return await _sqlClientSingleton.InsertAsync(query: threadQuery, parameters: threadParams);
    }

    public async Task DeleteThreadOrThrowAsync(int id)
    {
        SQLiteParameter[] parameters = [new("@ThreadId", id)];
        int rowsAffectedCount = await _sqlClientSingleton.DeleteAsync(query: "DELETE FROM DiscussionThreads WHERE Id_DiscussionThreads = @ThreadId", parameters: parameters);
        if (rowsAffectedCount == 0)
            throw new PublicErrorException($"Thread with id {id} not found");
    }

    public async Task<List<ThreadInitialMessageDbModel>> GetInitialMessageThreadsByLocationIdAsync(int locationId)
    {
        return await GetInitialMessageThreadsByKeyValueAsync(key: "LocationId", value: locationId.ToString());
    }

    public async Task<List<ThreadInitialMessageDbModel>> GetInitialMessageThreadsByAccountIdAsync(int accountId)
    {
        return await GetInitialMessageThreadsByKeyValueAsync(key: "AuthorId", value: accountId.ToString());
    }

    private async Task<List<ThreadInitialMessageDbModel>> GetInitialMessageThreadsByKeyValueAsync(string key, string value)
    {
        List<ThreadInitialMessageDbModel> list = new();
        string query = $"""
            SELECT
                dt.*, 
                u.Username, 
                COUNT(tm.Id_ThreadMessages) AS MessagesCount, 
                ml.Name, 
                initial_message.MessageText AS InitialMessageText,
                initial_message.DeletedAt as InitialMessageDeletedAt,
                initial_message.DeletedByAccountId as InitialMessageDeletedByAccountId,
                u2.Username as InitialMessageDeletedByUsername
            FROM DiscussionThreads dt
            LEFT JOIN Accounts u ON dt.AuthorId = u.Id_Accounts
            LEFT JOIN ThreadMessages tm ON dt.Id_DiscussionThreads = tm.ThreadId
            LEFT JOIN MapLocations ml ON dt.LocationId = ml.Id_MapLocations
            LEFT JOIN ThreadMessages initial_message ON 
                dt.Id_DiscussionThreads = initial_message.ThreadId AND 
                initial_message.IsInitialMessage = 1
            LEFT JOIN Accounts u2 ON initial_message.DeletedByAccountId = u2.Id_Accounts
            WHERE dt.{key} = @value
            GROUP BY dt.Id_DiscussionThreads
            ORDER BY dt.CreatedAt DESC
            """;
        SQLiteParameter[] parameters = [new("@value", value)];
        DataTable dt = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);
        foreach (DataRow row in dt.Rows)
            list.Add(item: MapRowToThreadInitialMessage(row: row));
        return list;
    }

    public async Task<List<ThreadMessageExtended>> GetMessagesByThreadIdAsync(int threadId)
    {
        List<ThreadMessageExtended> list = new();
        string query = """
            SELECT tm.*, u.Username AS AuthorUsername, u2.Username AS DeletedByUsername
            FROM ThreadMessages tm
            LEFT JOIN Accounts u ON tm.AuthorId = u.Id_Accounts
            LEFT JOIN Accounts u2 ON tm.DeletedByAccountId = u2.Id_Accounts
            WHERE ThreadId = @ThreadId ORDER BY CreatedAt
            """;
        SQLiteParameter[] parameters = [new("@ThreadId", threadId)];
        DataTable dt = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);
        foreach (DataRow row in dt.Rows)
            list.Add(item: MapRowToMessageGet(row: row));

        return list;
    }

    public async Task<ThreadMessageExtended> GetMessageByIdOrThrowAsync(int id)
    {
        string query = """
            SELECT tm.*, u.Username AS AuthorUsername, u2.Username AS DeletedByUsername
            FROM ThreadMessages tm
            LEFT JOIN Accounts u ON tm.AuthorId = u.Id_Accounts
            LEFT JOIN Accounts u2 ON tm.DeletedByAccountId = u2.Id_Accounts
            WHERE Id_ThreadMessages = @Id
            """;
        SQLiteParameter[] parameters = [new("@Id", id)];
        DataTable dt = await _sqlClientSingleton.SelectAsync(query: query, parameters: parameters);
        return dt.Rows.Count == 0
            ? throw new PublicErrorException($"Message with id {id} not found")
            : MapRowToMessageGet(row: dt.Rows[0]);
    }

    public async Task<int> CreateMessageAsync(
        int threadId,
        int authorId,
        string messageText,
        bool isInitialMessage
    )
    {
        DateTime now = DateTime.UtcNow;
        string query = "INSERT INTO ThreadMessages (ThreadId, AuthorId, MessageText, IsInitialMessage, CreatedAt) VALUES (@ThreadId, @AuthorId, @MessageText, @IsInitialMessage, @CreatedAt); SELECT last_insert_rowid();";
        SQLiteParameter[] parameters =
        [
            new("@ThreadId", threadId),
            new("@AuthorId", authorId),
            new("@MessageText", messageText),
            new("@IsInitialMessage", isInitialMessage),
            new("@CreatedAt", now)
        ];
        return await _sqlClientSingleton.InsertAsync(query: query, parameters: parameters);
    }

    public async Task DeleteMessageOrThrowAsync(int id, int deletedByAccountId)
    {
        _ = await _sqlClientSingleton.UpdateOrThrowAsync(query: "UPDATE ThreadMessages SET DeletedByAccountId=@DeletedByAccountId, DeletedAt=@DeletedAt WHERE Id_ThreadMessages=@Id", parameters: [new("@DeletedByAccountId", deletedByAccountId), new("@DeletedAt", DateTime.UtcNow), new("@Id", id)]);
    }

    public async Task ConvertReviewThreadToDiscussionOrThrowAsync(int threadId)
    {
        string query = "UPDATE DiscussionThreads SET ReviewId=null WHERE Id_DiscussionThreads=@ThreadId";
        _ = await _sqlClientSingleton.UpdateOrThrowAsync(query: query, parameters: [new("@ThreadId", threadId)]);
    }

    private static ThreadInitialMessageDbModel MapRowToThreadInitialMessage(DataRow row)
    {
        return new ThreadInitialMessageDbModel
        {
            Id = row.GetValueThrowNotPresentOrNull<int>(columnName: "Id_DiscussionThreads"),
            LocationId = row.GetValueThrowNotPresentOrNull<int>(columnName: "LocationId"),
            AuthorId = row.GetValueThrowNotPresentOrNull<int>(columnName: "AuthorId"),
            ThreadName = row.GetValueThrowNotPresentOrNull<string>(columnName: "ThreadName"),
            CreatedAt = row.GetValueThrowNotPresentOrNull<DateTime>(columnName: "CreatedAt"),
            ReviewId = row.GetValueThrowNotPresent<int?>(columnName: "ReviewId"),
            AuthorUsername = row.GetValueThrowNotPresentOrNull<string>(columnName: "Username"),
            IsAnonymous = row.GetValueThrowNotPresentOrNull<bool>(columnName: "IsAnonymous"),
            MessagesCount = row.GetValueThrowNotPresentOrNull<int>(columnName: "MessagesCount"),
            InitialMessageText = row.GetValueThrowNotPresentOrNull<string>(columnName: "InitialMessageText"),
            InitialMessageDeletedAt = row.GetValueThrowNotPresent<DateTime?>(columnName: "InitialMessageDeletedAt"),
            InitialMessageDeletedByAccountId = row.GetValueThrowNotPresent<int?>(columnName: "InitialMessageDeletedByAccountId"),
            InitialMessageDeletedByUsername = row.GetValueThrowNotPresent<string?>(columnName: "InitialMessageDeletedByUsername")
        };
    }

    private async Task<ThreadDisplayPageModel> MapRowToThreadDisplay(
        DataRow row
    )
    {
        int id = row.GetValueThrowNotPresentOrNull<int>(columnName: "Id_DiscussionThreads");
        return new ThreadDisplayPageModel
        {
            Id = id,
            LocationId = row.GetValueThrowNotPresentOrNull<int>(columnName: "LocationId"),
            AuthorId = row.GetValueThrowNotPresentOrNull<int>(columnName: "AuthorId"),
            ThreadName = row.GetValueThrowNotPresentOrNull<string>(columnName: "ThreadName"),
            CreatedAt = row.GetValueThrowNotPresentOrNull<DateTime>(columnName: "CreatedAt"),
            ReviewId = row.GetValueThrowNotPresent<int?>(columnName: "ReviewId"),
            AuthorUsername = row.GetValueThrowNotPresentOrNull<string>(columnName: "Username"),
            IsAnonymous = row.GetValueThrowNotPresentOrNull<bool>(columnName: "IsAnonymous"),
            Messages = [.. await GetMessagesByThreadIdAsync(threadId: id)],
            LocationName = row.GetValueThrowNotPresentOrNull<string>(columnName: "Name"),
            Rating = row.GetValueThrowNotPresent<int?>(columnName: "Rating")
        };
    }

    private static ThreadMessageExtended MapRowToMessageGet(DataRow row)
    {
        return new ThreadMessageExtended
        {
            Id = row.GetValueThrowNotPresentOrNull<int>(columnName: "Id_ThreadMessages"),
            ThreadId = row.GetValueThrowNotPresentOrNull<int>(columnName: "ThreadId"),
            AuthorId = row.GetValueThrowNotPresentOrNull<int>(columnName: "AuthorId"),
            MessageText = row.GetValueThrowNotPresentOrNull<string>(columnName: "MessageText"),
            IsInitialMessage = row.GetValueThrowNotPresentOrNull<bool>(columnName: "IsInitialMessage"),
            CreatedAt = row.GetValueThrowNotPresentOrNull<DateTime>(columnName: "CreatedAt"),
            DeletedAt = row.GetValueThrowNotPresent<DateTime?>(columnName: "DeletedAt"),
            AuthorUsername = row.GetValueThrowNotPresentOrNull<string>(columnName: "AuthorUsername"),
            DeletedByAccountId = row.GetValueThrowNotPresent<int?>(columnName: "DeletedByAccountId"),
            DeletedByUsername = row.GetValueThrowNotPresent<string?>(columnName: "DeletedByUsername")
        };
    }
}
