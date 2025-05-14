namespace MapHive.Repositories
{
    using MapHive.Models.RepositoryModels;

    public interface IDiscussionRepository
    {
        // Thread operations
        Task<List<DiscussionThreadGet>?> GetAllDiscussionThreadsByLocationIdAsync(int locationId);
        Task<DiscussionThreadGet?> GetThreadByIdAsync(int id);
        Task<DiscussionThreadGet> CreateDiscussionThreadAsync(DiscussionThreadCreate thread, string initialMessage);
        Task<DiscussionThreadGet> CreateReviewThreadAsync(ReviewThreadCreate threadCreate);
        Task<bool> DeleteThreadAsync(int id);
        Task<List<DiscussionThreadGet>> GetThreadsByAccountIdAsync(int accountId);

        // Message operations
        Task<List<ThreadMessageGet>> GetMessagesByThreadIdAsync(int threadId);
        Task<ThreadMessageGet?> GetMessageByIdAsync(int id);
        Task<ThreadMessageGet> AddMessageAsync(ThreadMessageCreate message);
        Task<bool> DeleteMessageAsync(int id, int deletedByAccountId);
        Task<bool> ConvertReviewThreadToDiscussionAsync(int threadId, string initialMessage);
    }
}