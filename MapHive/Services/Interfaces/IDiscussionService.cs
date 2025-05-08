using MapHive.Models.RepositoryModels;
using MapHive.Models.ViewModels;

namespace MapHive.Services
{
    public interface IDiscussionService
    {
        Task<ThreadDetailsViewModel> GetThreadDetailsAsync(int threadId);
        Task<DiscussionThreadViewModel> GetCreateModelAsync(int locationId);
        Task<DiscussionThreadGet> CreateDiscussionThreadAsync(DiscussionThreadViewModel model, int userId);
        Task<ThreadMessageGet> AddMessageAsync(ThreadMessageViewModel model, int userId);
        Task DeleteMessageAsync(int messageId, int userId, bool isAdmin);
        Task<int> DeleteThreadAsync(int threadId);
        /// <summary>
        /// Retrieves the view model for the thread page including details and new message form.
        /// </summary>
        Task<ThreadPageViewModel> GetThreadPageViewModelAsync(int threadId);
    }
}