namespace MapHive.Repositories
{
    using MapHive.Models.RepositoryModels;

    public interface IDiscussionRepository
    {
        // Thread operations
        Task<IEnumerable<DiscussionThreadGet>> GetDiscussionThreadsByLocationIdAsync(int locationId);
        Task<IEnumerable<DiscussionThreadGet>> GetAllDiscussionThreadsByLocationIdAsync(int locationId);
        Task<DiscussionThreadGet?> GetThreadByIdAsync(int id);
        Task<DiscussionThreadGet> CreateDiscussionThreadAsync(DiscussionThreadCreate thread, string initialMessage);
        Task<DiscussionThreadGet> CreateReviewThreadAsync(ReviewThreadCreate threadCreate);
        Task<bool> DeleteThreadAsync(int id);
        Task<IEnumerable<DiscussionThreadGet>> GetThreadsByUserIdAsync(int userId);

        // Message operations
        Task<IEnumerable<ThreadMessageGet>> GetMessagesByThreadIdAsync(int threadId);
        Task<ThreadMessageGet?> GetMessageByIdAsync(int id);
        Task<ThreadMessageGet> AddMessageAsync(ThreadMessageCreate message);
        Task<bool> DeleteMessageAsync(int id, int deletedByUserId);
        Task<bool> ConvertReviewThreadToDiscussionAsync(int threadId, string initialMessage);
    }
}