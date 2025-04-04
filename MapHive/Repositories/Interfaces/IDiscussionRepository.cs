using MapHive.Models;

namespace MapHive.Repositories
{
    public interface IDiscussionRepository
    {
        // Thread operations
        Task<IEnumerable<DiscussionThread>> GetDiscussionThreadsByLocationIdAsync(int locationId);
        Task<IEnumerable<DiscussionThread>> GetAllDiscussionThreadsByLocationIdAsync(int locationId);
        Task<DiscussionThread?> GetThreadByIdAsync(int id);
        Task<DiscussionThread> CreateDiscussionThreadAsync(DiscussionThread thread, string initialMessage);
        Task<DiscussionThread> CreateReviewThreadAsync(int reviewId, string reviewTitle, int locationId);
        Task<bool> DeleteThreadAsync(int id);
        Task<IEnumerable<DiscussionThread>> GetThreadsByUserIdAsync(int userId);

        // Message operations
        Task<IEnumerable<ThreadMessage>> GetMessagesByThreadIdAsync(int threadId);
        Task<ThreadMessage?> GetMessageByIdAsync(int id);
        Task<ThreadMessage> AddMessageAsync(ThreadMessage message);
        Task<bool> DeleteMessageAsync(int id, int deletedByUserId);
        Task<bool> ConvertReviewThreadToDiscussionAsync(int threadId, string initialMessage);
    }
}