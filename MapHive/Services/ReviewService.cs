namespace MapHive.Services;

using System.Data;
using MapHive.Models.Data;
using MapHive.Models.Data.DbTableModels;
using MapHive.Models.Exceptions;
using MapHive.Models.PageModels;
using MapHive.Repositories;

public class ReviewService(
    IReviewRepository reviewRepository,
    IMapLocationRepository mapRepository,
    IUserContextService userContextService,
    IDiscussionService discussionService,
    IDiscussionRepository discussionRepository) : IReviewService
{
    private readonly IReviewRepository _reviewRepository = reviewRepository;
    private readonly IMapLocationRepository _mapRepository = mapRepository;
    private readonly IUserContextService _userContextService = userContextService;
    private readonly IDiscussionService _discussionService = discussionService;
    private readonly IDiscussionRepository _discussionRepository = discussionRepository;

    public async Task<int> CreateReviewAsync(int locationId, ReviewUpdatePageModel model)
    {
        if (await _reviewRepository.HasUserReviewedLocationAsync(accountId: _userContextService.AccountIdOrThrow, locationId: locationId))
            throw new PublicWarningException($"User \"{_userContextService.AccountIdOrThrow}\" has already reviewed location with id \"{locationId}\".");

        int reviewId = await _reviewRepository.CreateReviewAsync(
            locationId: locationId,
            accountId: _userContextService.AccountIdOrThrow,
            rating: model.Rating ?? throw new NoNullAllowedException(nameof(model.Rating)),
            isAnonymous: model.IsAnonymous
        );

        _ = await _discussionService.CreateDiscussionThreadAsync(
            locationId: locationId,
            threadName: $"{(model.IsAnonymous ? "Anonymous" : _userContextService.UsernameOrThrow + "'s")} review of {model.LocationName}",
            reviewId: reviewId,
            isAnonymous: model.IsAnonymous,
            initialMessage: model.ReviewText ?? throw new PublicErrorException("Review text cannot be empty")
        );

        return reviewId;
    }

    public async Task<ReviewUpdatePageModel> GetEditModelAsync(int reviewId)
    {
        ReviewExtended review = await _reviewRepository.GetReviewByIdOrThrowAsync(id: reviewId);
        return review.AccountId != _userContextService.AccountIdOrThrow && !_userContextService.IsAdminOrThrow
            ? throw new UnauthorizedAccessException()
            : new()
            {
                LocationName = (await _mapRepository.GetLocationByIdOrThrowAsync(id: review.LocationId)).Name,
                IsAnonymous = review.IsAnonymous,
                Rating = review.Rating,
                ReviewText = review.ReviewText,
            };
    }

    public async Task<int> EditReviewAsync(int id, ReviewUpdatePageModel model)
    {
        ReviewExtended review = await _reviewRepository.GetReviewByIdOrThrowAsync(id: id);
        if (review.AccountId != _userContextService.AccountIdOrThrow)
        {
            throw new UnauthorizedAccessException();
        }

        await _reviewRepository.UpdateReviewOrThrowAsync(
            id: id,
            rating: DynamicValue<int>.Set(model.Rating ?? throw new NoNullAllowedException(nameof(model.Rating))),
            reviewText: DynamicValue<string>.Set(model.ReviewText ?? throw new NoNullAllowedException(nameof(model.ReviewText))),
            isAnonymous: DynamicValue<bool>.Set(model.IsAnonymous)
        );
        return review.LocationId;
    }

    /// <summary>Returns location id</summary>
    public async Task<int> DeleteReviewAsync(int id)
    {
        ReviewExtended review = await _reviewRepository.GetReviewByIdOrThrowAsync(id: id);
        if (!_userContextService.IsAdminOrThrow && review.AccountId != review.AccountId)
            throw new UnauthorizedAccessException();

        ThreadDisplayPageModel thread = await _discussionRepository.GetThreadByReviewIdOrThrowAsync(reviewId: review.Id);
        if (!thread.ReviewId.HasValue)
            throw new Exception($"Review thread \"{thread.Id}\" not found");
        List<ThreadMessageExtended> messages = await _discussionRepository.GetMessagesByThreadIdAsync(thread.Id);
        if (messages.Count > 1)
        {
            await _discussionRepository.ConvertReviewThreadToDiscussionOrThrowAsync(threadId: thread.Id);
        }
        else
        {
            await _discussionRepository.DeleteThreadOrThrowAsync(id: thread.Id);
        }

        int locationId = review.LocationId;
        await _reviewRepository.DeleteReviewOrThrowAsync(id: id);

        return locationId;
    }
}
