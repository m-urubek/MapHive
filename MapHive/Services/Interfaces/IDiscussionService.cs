namespace MapHive.Services;

using MapHive.Models.Data.DbTableModels;
using MapHive.Models.PageModels;

public interface IDiscussionService
{
    Task<ThreadDisplayPageModel> GetThreadDisplayPageModelAsync(int threadId);
    Task DeleteMessageAsync(int messageId);
    Task<ThreadCreatePageModel> GetThreadCreatePageModelAsync(int locationId);

    public Task<List<ThreadInitialMessageDbModel>> GetInitialMessageThreadsPageModelByAccountIdAsync(int accountId);

    public Task<int> CreateDiscussionThreadAsync(
        int locationId,
        string threadName,
        int? reviewId,
        bool isAnonymous,
        string initialMessage
    );
}