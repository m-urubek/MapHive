namespace MapHive.Services;

using System.Collections.Generic;
using System.Data;
using System.Linq;
using MapHive.Models.Data;
using MapHive.Models.Data.DbTableModels;
using MapHive.Models.Exceptions;
using MapHive.Models.PageModels;
using MapHive.Repositories;

public class MapLocationService(
    IMapLocationRepository _mapRepository,
    IDiscussionRepository _discussionRepository,
    IReviewRepository _reviewRepository,
    IUserContextService _userContextService,
    IUsernamesService _usernamesService) : IMapLocationService
{

    public async Task<LocationExtended> GetLocationByIdOrThrowAsync(int id)
    {
        LocationExtended? location = await _mapRepository.GetLocationByIdAsync(id: id);
        return location ?? throw new PublicErrorException($"Location {id} not found");
    }

    public async Task UpdateLocationOrThrowAsync(int locationId, LocationUpdatePageModel locationUpdatePageModel)
    {
        LocationExtended mapLocationGet = await _mapRepository.GetLocationByIdOrThrowAsync(id: locationId);
        EnsureUserCanEditLocation(mapLocationGet.OwnerId);
        await _mapRepository.UpdateLocationOrThrowAsync(
            id: locationId,
            name: DynamicValue<string>.Set(locationUpdatePageModel.Name ?? throw new NoNullAllowedException(nameof(locationUpdatePageModel.Name))),
            description: DynamicValue<string?>.Set(locationUpdatePageModel.Description),
            latitude: DynamicValue<double>.Set(locationUpdatePageModel.Latitude ?? throw new NoNullAllowedException(nameof(locationUpdatePageModel.Latitude))),
            longitude: DynamicValue<double>.Set(locationUpdatePageModel.Longitude ?? throw new NoNullAllowedException(nameof(locationUpdatePageModel.Longitude))),
            address: DynamicValue<string?>.Set(locationUpdatePageModel.Address),
            website: DynamicValue<string?>.Set(locationUpdatePageModel.Website),
            phoneNumber: DynamicValue<string?>.Set(locationUpdatePageModel.PhoneNumber),
            isAnonymous: DynamicValue<bool>.Set(locationUpdatePageModel.IsAnonymous),
            categoryId: DynamicValue<int>.Set(locationUpdatePageModel.CategoryId ?? throw new NoNullAllowedException(nameof(locationUpdatePageModel.CategoryId)))
        );
    }

    public async Task<bool> DeleteLocationAsync(int id)
    {
        LocationExtended mapLocationGet = await _mapRepository.GetLocationByIdOrThrowAsync(id: id);
        EnsureUserCanEditLocation(mapLocationGet.OwnerId);
        return await _mapRepository.DeleteLocationAsync(id: id);
    }

    //todo decompose
    public async Task<LocationDisplayPageModel> GetLocationDetailsAsync(int id)
    {
        LocationExtended location = await _mapRepository.GetLocationByIdOrThrowAsync(id: id);

        (string ownerUsername, int? ownerId) = _usernamesService.GetAnonymizedUser(
            username: location.OwnerUsername,
            authorId: location.OwnerId,
            isAnonymous: location.IsAnonymous
        );

        List<ReviewExtended>? reviews = await _reviewRepository.GetReviewsByLocationIdAsync(locationId: id);
        int reviewCount = reviews is null ? 0 : reviews.Count;
        if (reviews != null)
        {
            foreach (ReviewExtended review in reviews)
                (review.AuthorUsername, review.AccountId) = _usernamesService.GetAnonymizedUser(username: review.AuthorUsername, authorId: review.AccountId, isAnonymous: review.IsAnonymous);
        }

        // Retrieve all discussion threads (including review threads) for review section, and count non-review threads for general discussions
        List<ThreadInitialMessageDbModel>? allThreads = await _discussionRepository.GetInitialMessageThreadsByLocationIdAsync(locationId: id);

        // Sanitize anonymous discussion threads for non-admin users
        if (allThreads != null)
        {
            foreach (ThreadInitialMessageDbModel thread in allThreads)
            {
                (string threadAuthorUsername, int? threadAuthorId) = _usernamesService.GetAnonymizedUser(
                    username: thread.AuthorUsername,
                    authorId: thread.AuthorId,
                    isAnonymous: thread.IsAnonymous
                );
                thread.AuthorUsername = threadAuthorUsername;
                thread.AuthorId = threadAuthorId;
            }
        }

        // Determine if the current user can edit this location
        bool canEdit = _userContextService.IsAuthenticated && (location.OwnerId == _userContextService.AccountIdOrThrow || _userContextService.IsAdminOrThrow);

        return new()
        {
            Name = location.Name,
            Description = location.Description,
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            Address = location.Address,
            Website = location.Website,
            PhoneNumber = location.PhoneNumber,
            IsAnonymous = location.IsAnonymous,
            CategoryName = location.CategoryName,
            CreatedAt = location.CreatedAt,
            UpdatedAt = location.UpdatedAt,
            OwnerUsername = ownerUsername,
            OwnerId = ownerId,
            HasReviewed = _userContextService.IsAuthenticated && (reviews?.Select(r => r.AccountId).Contains(_userContextService.AccountIdOrThrow) ?? false),
            RegularDiscussionCount = allThreads?.Count(d => !d.ReviewId.HasValue) ?? 0,
            Reviews = reviews?.Select(review => new ReviewDisplayPageModel()
            {
                Id = review.Id,
                IsAnonymous = review.IsAnonymous,
                Rating = review.Rating,
                AuthorUsername = review.AuthorUsername,
                AuthorId = review.AccountId,
                CreatedAt = review.CreatedAt,
                ReviewText = review.ReviewText,
            }).ToList(),
            Threads = allThreads,
            CanEdit = canEdit,
        };
    }

    /// <summary>
    /// Retrieves data for the Add Location page, including categories.
    /// </summary>
    public async Task<LocationUpdatePageModel> GetAddLocationPagePageModelAsync()
    {
        return new LocationUpdatePageModel
        {
            Categories = await _mapRepository.GetAllCategoriesAsync(),
            Name = null,
            Description = null,
            Latitude = 50.09110453895419,
            Longitude = 14.40161930844168,
            Address = null,
            Website = null,
            PhoneNumber = null,
            IsAnonymous = false,
            CategoryId = null,
        };
    }

    /// <summary>
    /// Retrieves data for the Edit Location page, including current values and categories.
    /// </summary>
    public async Task<LocationUpdatePageModel> GetLocationUpdatePageModelAsync(int id)
    {
        LocationExtended mapLocationGet = await _mapRepository.GetLocationByIdOrThrowAsync(id: id);
        EnsureUserCanEditLocation(mapLocationGet.OwnerId);
        return new LocationUpdatePageModel()
        {
            Categories = await _mapRepository.GetAllCategoriesAsync(),
            Name = mapLocationGet.Name,
            Description = mapLocationGet.Description,
            Latitude = mapLocationGet.Latitude,
            Longitude = mapLocationGet.Longitude,
            Address = mapLocationGet.Address,
            Website = mapLocationGet.Website,
            PhoneNumber = mapLocationGet.PhoneNumber,
            IsAnonymous = mapLocationGet.IsAnonymous,
            CategoryId = mapLocationGet.CategoryId,
        };
    }

    public void EnsureUserCanEditLocation(int locationAuthorId)
    {
        _userContextService.EnsureAuthenticated();
        if (locationAuthorId != _userContextService.AccountIdOrThrow && !_userContextService.IsAdminOrThrow)
            throw new UnauthorizedAccessException("User is not allowed to update this location");
    }
}
