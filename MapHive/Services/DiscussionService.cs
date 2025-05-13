namespace MapHive.Services
{
    using AutoMapper;
    using MapHive.Models.RepositoryModels;
    using MapHive.Models.ViewModels;
    using MapHive.Repositories;

    public class DiscussionService(
        IDiscussionRepository discussionRepository,
        IMapLocationRepository mapRepository,
        IReviewRepository reviewRepository,
        IMapper mapper,
        IUserContextService userContextService) : IDiscussionService
    {
        private readonly IDiscussionRepository _discussionRepository = discussionRepository;
        private readonly IMapLocationRepository _mapRepository = mapRepository;
        private readonly IReviewRepository _reviewRepository = reviewRepository;
        private readonly IMapper _mapper = mapper;
        private readonly IUserContextService _userContextService = userContextService;

        public async Task<ThreadDetailsViewModel> GetThreadDetailsAsync(int threadId)
        {
            DiscussionThreadGet dto = await _discussionRepository.GetThreadByIdAsync(id: threadId)
                ?? throw new KeyNotFoundException($"Thread {threadId} not found");
            ThreadDetailsViewModel viewModel = _mapper.Map<ThreadDetailsViewModel>(source: dto);
            viewModel.Messages = await _discussionRepository.GetMessagesByThreadIdAsync(threadId: threadId);
            viewModel.Location = await _mapRepository.GetLocationByIdAsync(id: dto.LocationId)
                ?? throw new Exception("Location not found");
            if (dto.IsReviewThread && dto.ReviewId.HasValue)
            {
                viewModel.Review = await _reviewRepository.GetReviewByIdAsync(id: dto.ReviewId.Value);
            }
            return viewModel;
        }

        public async Task<DiscussionThreadViewModel> GetCreateModelAsync(int locationId)
        {
            MapLocationGet location = await _mapRepository.GetLocationByIdAsync(id: locationId)
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
            DiscussionThreadCreate dto = _mapper.Map<DiscussionThreadCreate>(source: model);
            dto.UserId = userId;
            dto.IsReviewThread = false;
            dto.ReviewId = null;
            return _discussionRepository.CreateDiscussionThreadAsync(thread: dto, initialMessage: model.InitialMessage);
        }

        public Task<ThreadMessageGet> AddMessageAsync(ThreadMessageViewModel model, int userId)
        {
            ThreadMessageCreate dto = _mapper.Map<ThreadMessageCreate>(source: model);
            dto.UserId = userId;
            dto.IsInitialMessage = false;
            return _discussionRepository.AddMessageAsync(message: dto);
        }

        public async Task DeleteMessageAsync(int messageId, int userId)
        {
            ThreadMessageGet message = await _discussionRepository.GetMessageByIdAsync(id: messageId)
                ?? throw new KeyNotFoundException($"Message {messageId} not found");
            if (!_userContextService.IsAdminRequired && message.UserId != userId)
            {
                throw new UnauthorizedAccessException();
            }

            _ = await _discussionRepository.DeleteMessageAsync(id: messageId, deletedByUserId: userId);
        }

        public async Task<int> DeleteThreadAsync(int threadId)
        {
            DiscussionThreadGet thread = await _discussionRepository.GetThreadByIdAsync(id: threadId)
                ?? throw new KeyNotFoundException($"Thread {threadId} not found");
            int locId = thread.LocationId;
            _ = await _discussionRepository.DeleteThreadAsync(id: threadId);
            return locId;
        }

        /// <summary>
        /// Retrieves the view model for the thread page including thread details and new message form.
        /// </summary>
        public async Task<ThreadPageViewModel> GetThreadPageViewModelAsync(int threadId)
        {
            // Get thread details
            ThreadDetailsViewModel threadDetails = await GetThreadDetailsAsync(threadId: threadId);
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
