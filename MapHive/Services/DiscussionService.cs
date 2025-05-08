using AutoMapper;
using MapHive.Models.RepositoryModels;
using MapHive.Models.ViewModels;
using MapHive.Repositories;

namespace MapHive.Services
{
    public class DiscussionService : IDiscussionService
    {
        private readonly IDiscussionRepository _discussionRepo;
        private readonly IMapLocationRepository _mapRepo;
        private readonly IReviewRepository _reviewRepo;
        private readonly IMapper _mapper;

        public DiscussionService(
            IDiscussionRepository discussionRepo,
            IMapLocationRepository mapRepo,
            IReviewRepository reviewRepo,
            IMapper mapper)
        {
            this._discussionRepo = discussionRepo;
            this._mapRepo = mapRepo;
            this._reviewRepo = reviewRepo;
            this._mapper = mapper;
        }

        public async Task<ThreadDetailsViewModel> GetThreadDetailsAsync(int threadId)
        {
            DiscussionThreadGet dto = await this._discussionRepo.GetThreadByIdAsync(threadId)
                ?? throw new KeyNotFoundException($"Thread {threadId} not found");
            ThreadDetailsViewModel viewModel = this._mapper.Map<ThreadDetailsViewModel>(dto);
            viewModel.Messages = (await this._discussionRepo.GetMessagesByThreadIdAsync(threadId)).ToList();
            viewModel.Location = await this._mapRepo.GetLocationByIdAsync(dto.LocationId)
                ?? throw new Exception("Location not found");
            if (dto.IsReviewThread && dto.ReviewId.HasValue)
            {
                viewModel.Review = await this._reviewRepo.GetReviewByIdAsync(dto.ReviewId.Value);
            }
            return viewModel;
        }

        public async Task<DiscussionThreadViewModel> GetCreateModelAsync(int locationId)
        {
            MapLocationGet location = await this._mapRepo.GetLocationByIdAsync(locationId)
                ?? throw new KeyNotFoundException($"Location {locationId} not found");
            return new DiscussionThreadViewModel
            {
                LocationId = locationId,
                LocationName = location.Name,
                ThreadName = string.Empty,
                InitialMessage = string.Empty
            };
        }

        public Task<DiscussionThreadGet> CreateDiscussionThreadAsync(DiscussionThreadViewModel model, int userId)
        {
            DiscussionThreadCreate dto = this._mapper.Map<DiscussionThreadCreate>(model);
            dto.UserId = userId;
            dto.IsReviewThread = false;
            dto.ReviewId = null;
            return this._discussionRepo.CreateDiscussionThreadAsync(dto, model.InitialMessage);
        }

        public Task<ThreadMessageGet> AddMessageAsync(ThreadMessageViewModel model, int userId)
        {
            ThreadMessageCreate dto = this._mapper.Map<ThreadMessageCreate>(model);
            dto.UserId = userId;
            dto.IsInitialMessage = false;
            return this._discussionRepo.AddMessageAsync(dto);
        }

        public async Task DeleteMessageAsync(int messageId, int userId, bool isAdmin)
        {
            ThreadMessageGet message = await this._discussionRepo.GetMessageByIdAsync(messageId)
                ?? throw new KeyNotFoundException($"Message {messageId} not found");
            if (!isAdmin && message.UserId != userId)
            {
                throw new UnauthorizedAccessException();
            }

            _ = await this._discussionRepo.DeleteMessageAsync(messageId, userId);
        }

        public async Task<int> DeleteThreadAsync(int threadId)
        {
            DiscussionThreadGet thread = await this._discussionRepo.GetThreadByIdAsync(threadId)
                ?? throw new KeyNotFoundException($"Thread {threadId} not found");
            int locId = thread.LocationId;
            _ = await this._discussionRepo.DeleteThreadAsync(threadId);
            return locId;
        }

        /// <summary>
        /// Retrieves the view model for the thread page including thread details and new message form.
        /// </summary>
        public async Task<ThreadPageViewModel> GetThreadPageViewModelAsync(int threadId)
        {
            // Get thread details
            ThreadDetailsViewModel threadDetails = await this.GetThreadDetailsAsync(threadId);
            // Prepare new message model
            ThreadMessageViewModel newMessage = new()
            {
                ThreadId = threadId,
                ThreadName = threadDetails.ThreadName,
                MessageText = string.Empty
            };
            return new ThreadPageViewModel
            {
                ThreadDetails = threadDetails,
                NewMessage = newMessage
            };
        }
    }
}