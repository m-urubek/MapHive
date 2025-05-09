using AutoMapper;
using MapHive.Models.Exceptions;
using MapHive.Models.RepositoryModels;
using MapHive.Models.ViewModels;
using MapHive.Repositories;

namespace MapHive.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IReviewRepository _reviewRepository;
        private readonly IMapLocationRepository _mapRepository;
        private readonly IDiscussionRepository _discussionRepository;
        private readonly IMapper _mapper;

        public ReviewService(
            IReviewRepository reviewRepository,
            IMapLocationRepository mapRepository,
            IDiscussionRepository discussionRepository,
            IMapper mapper)
        {
            this._reviewRepository = reviewRepository;
            this._mapRepository = mapRepository;
            this._discussionRepository = discussionRepository;
            this._mapper = mapper;
        }

        public async Task<ReviewViewModel> GetCreateModelAsync(int locationId)
        {
            MapLocationGet location = await this._mapRepository.GetLocationByIdAsync(locationId)
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
            if (await this._reviewRepository.HasUserReviewedLocationAsync(userId, model.LocationId))
            {
                throw new OrangeUserException("You have already reviewed this location.");
            }

            ReviewCreate reviewDto = this._mapper.Map<ReviewCreate>(model);
            reviewDto.UserId = userId;
            ReviewGet createdReview = await this._reviewRepository.AddReviewAsync(reviewDto);

            ReviewThreadCreate threadDto = new()
            {
                ReviewId = createdReview.Id,
                LocationId = model.LocationId,
                UserId = userId,
                Username = string.Empty,
                ReviewTitle = model.LocationName
            };
            _ = await this._discussionRepository.CreateReviewThreadAsync(threadDto);

            return createdReview;
        }

        public async Task<ReviewViewModel> GetEditModelAsync(int reviewId, int userId)
        {
            ReviewGet review = await this._reviewRepository.GetReviewByIdAsync(reviewId)
                ?? throw new KeyNotFoundException($"Review {reviewId} not found");
            if (review.UserId != userId)
            {
                throw new UnauthorizedAccessException();
            }
            ReviewViewModel model = this._mapper.Map<ReviewViewModel>(review);
            MapLocationGet location = await this._mapRepository.GetLocationByIdAsync(review.LocationId)
                ?? throw new KeyNotFoundException($"Location {review.LocationId} not found");
            model.LocationName = location.Name;
            return model;
        }

        public async Task EditReviewAsync(int id, ReviewViewModel model, int userId)
        {
            ReviewGet review = await this._reviewRepository.GetReviewByIdAsync(id)
                ?? throw new KeyNotFoundException($"Review {id} not found");
            if (review.UserId != userId)
            {
                throw new UnauthorizedAccessException();
            }

            ReviewUpdate updateDto = this._mapper.Map<ReviewUpdate>(model);
            updateDto.Id = id;
            updateDto.UserId = userId;
            updateDto.LocationId = review.LocationId;

            if (!await this._reviewRepository.UpdateReviewAsync(updateDto))
            {
                throw new Exception("Failed to update review.");
            }
        }

        public async Task<int> DeleteReviewAsync(int id, int userId, bool isAdmin)
        {
            ReviewGet review = await this._reviewRepository.GetReviewByIdAsync(id)
                ?? throw new KeyNotFoundException($"Review {id} not found");
            if (!isAdmin && review.UserId != userId)
            {
                throw new UnauthorizedAccessException();
            }

            string reviewText = review.ReviewText;
            int locationId = review.LocationId;

            _ = await this._reviewRepository.DeleteReviewAsync(id);
            DiscussionThreadGet? thread = await this._discussionRepository.GetThreadByIdAsync(review.Id);
            if (thread != null && thread.IsReviewThread && thread.ReviewId == review.Id)
            {
                IEnumerable<ThreadMessageGet> messages = await this._discussionRepository.GetMessagesByThreadIdAsync(thread.Id);
                if (messages.Any())
                {
                    _ = await this._discussionRepository.ConvertReviewThreadToDiscussionAsync(thread.Id, reviewText);
                }
            }
            return locationId;
        }
    }
}