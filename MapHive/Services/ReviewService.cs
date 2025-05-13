namespace MapHive.Services
{
    using AutoMapper;
    using MapHive.Models.Exceptions;
    using MapHive.Models.RepositoryModels;
    using MapHive.Models.ViewModels;
    using MapHive.Repositories;

    public class ReviewService(
        IReviewRepository reviewRepository,
        IMapLocationRepository mapRepository,
        IDiscussionRepository discussionRepository,
        IMapper mapper,
        IUserContextService userContextService) : IReviewService
    {
        private readonly IReviewRepository _reviewRepository = reviewRepository;
        private readonly IMapLocationRepository _mapRepository = mapRepository;
        private readonly IDiscussionRepository _discussionRepository = discussionRepository;
        private readonly IMapper _mapper = mapper;
        private readonly IUserContextService _userContextService = userContextService;

        public async Task<ReviewViewModel> GetCreateModelAsync(int locationId)
        {
            MapLocationGet location = await _mapRepository.GetLocationByIdAsync(id: locationId)
                ?? throw new KeyNotFoundException($"Location {locationId} not found");
            return new ReviewViewModel
            {
                LocationId = locationId,
                LocationName = location.Name,
                Rating = 0,
                ReviewText = string.Empty
            };
        }

        public async Task<ReviewGet> CreateReviewAsync(ReviewViewModel model, int userId)
        {
            if (await _reviewRepository.HasUserReviewedLocationAsync(userId: userId, locationId: model.LocationId))
            {
                throw new OrangeUserException("You have already reviewed this location.");
            }

            ReviewCreate reviewDto = _mapper.Map<ReviewCreate>(source: model);
            reviewDto.UserId = userId;
            ReviewGet createdReview = await _reviewRepository.AddReviewAsync(review: reviewDto);

            ReviewThreadCreate threadDto = new()
            {
                ReviewId = createdReview.Id,
                LocationId = model.LocationId,
                UserId = userId,
                Username = string.Empty,
                ReviewTitle = model.LocationName
            };
            _ = await _discussionRepository.CreateReviewThreadAsync(threadCreate: threadDto);

            return createdReview;
        }

        public async Task<ReviewViewModel> GetEditModelAsync(int reviewId, int userId)
        {
            ReviewGet review = await _reviewRepository.GetReviewByIdAsync(id: reviewId)
                ?? throw new KeyNotFoundException($"Review {reviewId} not found");
            if (review.UserId != userId)
            {
                throw new UnauthorizedAccessException();
            }
            ReviewViewModel model = _mapper.Map<ReviewViewModel>(source: review);
            MapLocationGet location = await _mapRepository.GetLocationByIdAsync(id: review.LocationId)
                ?? throw new KeyNotFoundException($"Location {review.LocationId} not found");
            model.LocationName = location.Name;
            return model;
        }

        public async Task EditReviewAsync(int id, ReviewViewModel model, int userId)
        {
            ReviewGet review = await _reviewRepository.GetReviewByIdAsync(id: id)
                ?? throw new KeyNotFoundException($"Review {id} not found");
            if (review.UserId != userId)
            {
                throw new UnauthorizedAccessException();
            }

            ReviewUpdate updateDto = _mapper.Map<ReviewUpdate>(source: model);
            updateDto.Id = id;
            updateDto.UserId = userId;
            updateDto.LocationId = review.LocationId;

            if (!await _reviewRepository.UpdateReviewAsync(review: updateDto))
            {
                throw new Exception("Failed to update review.");
            }
        }

        public async Task<int> DeleteReviewAsync(int id, int userId)
        {
            ReviewGet review = await _reviewRepository.GetReviewByIdAsync(id: id)
                ?? throw new KeyNotFoundException($"Review {id} not found");
            if (!_userContextService.IsAdminRequired && review.UserId != userId)
            {
                throw new UnauthorizedAccessException();
            }

            string reviewText = review.ReviewText;
            int locationId = review.LocationId;

            _ = await _reviewRepository.DeleteReviewAsync(id: id);
            DiscussionThreadGet? thread = await _discussionRepository.GetThreadByIdAsync(id: review.Id);
            if (thread != null && thread.IsReviewThread && thread.ReviewId == review.Id)
            {
                IEnumerable<ThreadMessageGet> messages = await _discussionRepository.GetMessagesByThreadIdAsync(threadId: thread.Id);
                if (messages.Any())
                {
                    _ = await _discussionRepository.ConvertReviewThreadToDiscussionAsync(threadId: thread.Id, initialMessage: reviewText);
                }
            }
            return locationId;
        }
    }
}