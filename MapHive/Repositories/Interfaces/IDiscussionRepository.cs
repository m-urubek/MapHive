namespace MapHive.Repositories;

using MapHive.Models.Data.DbTableModels;
using MapHive.Models.PageModels;

public interface IDiscussionRepository
{
    Task DeleteThreadOrThrowAsync(int id);
    Task<List<ThreadInitialMessageDbModel>> GetInitialMessageThreadsByAccountIdAsync(int accountId);
    Task<List<ThreadInitialMessageDbModel>> GetInitialMessageThreadsByLocationIdAsync(int locationId);
    Task<ThreadDisplayPageModel> GetThreadByReviewIdOrThrowAsync(int reviewId);
    Task<ThreadDisplayPageModel> GetThreadByIdOrThrowAsync(int id);

    Task<int> CreateDiscussionThreadAsync(
        int locationId,
        int accountId,
        string threadName,
        int? reviewId,
        bool isAnonymous
    );

    // Message operations
    Task<List<ThreadMessageExtended>> GetMessagesByThreadIdAsync(int threadId);
    Task<ThreadMessageExtended> GetMessageByIdOrThrowAsync(int id);
    Task<int> CreateMessageAsync(
        int threadId,
        int authorId,
        string messageText,
        bool isInitialMessage
    );
    Task DeleteMessageOrThrowAsync(int id, int deletedByAccountId);
    Task ConvertReviewThreadToDiscussionOrThrowAsync(int threadId);
}
