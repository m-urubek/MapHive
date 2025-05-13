namespace MapHive.Services
{
    using System.Collections.Generic;
    using System.Linq;
    using AutoMapper;
    using MapHive.Models.Exceptions;
    using MapHive.Models.RepositoryModels;
    using MapHive.Models.ViewModels;
    using MapHive.Repositories;

    public class MapLocationService(
        IMapLocationRepository mapRepository,
        IDiscussionRepository discussionRepository,
        IReviewRepository reviewRepository,
        IUserRepository userRepository,
        IMapper mapper,
        IUserContextService userContextService) : IMapService
    {
        private readonly IMapLocationRepository _mapRepository = mapRepository;
        private readonly IDiscussionRepository _discussionRepository = discussionRepository;
        private readonly IReviewRepository _reviewRepository = reviewRepository;
        private readonly IUserRepository _userRepository = userRepository;
        private readonly IMapper _mapper = mapper;
        private readonly IUserContextService _userContextService = userContextService;

        public Task<IEnumerable<MapLocationGet>> GetAllLocationsAsync()
        {
            return _mapRepository.GetAllLocationsAsync();
        }

        public Task<IEnumerable<CategoryGet>> GetAllCategoriesAsync()
        {
            return _mapRepository.GetAllCategoriesAsync();
        }

        public Task<MapLocationGet?> GetLocationByIdAsync(int id)
        {
            return _mapRepository.GetLocationByIdAsync(id: id);
        }

        public async Task<MapLocationGet> GetLocationByIdOrThrowAsync(int id)
        {
            MapLocationGet? location = await GetLocationByIdAsync(id: id);
            return location ?? throw new PublicErrorException($"Location {id} not found");
        }

        public async Task<MapLocationGet> AddLocationAsync(MapLocationCreate createDto)
        {
            createDto.UserId = _userContextService.UserIdRequired;
            return await _mapRepository.AddLocationAsync(location: createDto);
        }

        public async Task<MapLocationGet?> UpdateLocationAsync(int id, MapLocationUpdate updateDto)
        {
            MapLocationGet mapLocationGet = await _mapRepository.GetLocationByIdOrThrowAsync(id: id);
            EnsureUserCanEditLocation(mapLocationGet.UserId);
            return await _mapRepository.UpdateLocationAsync(location: updateDto);
        }

        public async Task<bool> DeleteLocationAsync(int id)
        {
            MapLocationGet mapLocationGet = await _mapRepository.GetLocationByIdOrThrowAsync(id: id);
            EnsureUserCanEditLocation(mapLocationGet.UserId);
            return await _mapRepository.DeleteLocationAsync(id: id);
        }

        public async Task<MapLocationViewModel> GetLocationDetailsAsync(int id)
        {
            MapLocationGet location = await _mapRepository.GetLocationWithCategoryOrThrowAsync(id: id);
            string authorUsername;

            if (_userContextService.IsAuthenticatedAndAdmin)
            {
                UserGet user = await _userRepository.GetUserByIdOrThrowAsync(id: location.UserId);
                authorUsername = user.Username + " (anonymous)";
            }
            else
            {
                if (!location.IsAnonymous)
                {
                    UserGet user = await _userRepository.GetUserByIdOrThrowAsync(id: location.UserId);
                    authorUsername = user.Username;
                }
                else
                {
                    authorUsername = "Anonymous";
                }
            }

            List<ReviewGet>? reviews = await _reviewRepository.GetReviewsByLocationIdAsync(locationId: id);
            int reviewCount = reviews is null ? 0 : reviews.Count;

            // Retrieve all discussion threads (including review threads) for review section, and count non-review threads for general discussions
            List<DiscussionThreadGet>? allThreads = await _discussionRepository.GetAllDiscussionThreadsByLocationIdAsync(locationId: id);

            return new()
            {
                MapLocationGet = location,
                AuthorUsername = authorUsername,
                ReviewCount = reviewCount,
                HasReviewed = await HasCurrentUserReviewedLocationAsync(locationId: id),
                RegularDiscussionCount = allThreads is null ? 0 : allThreads.Count(d => !d.IsReviewThread),
                Reviews = reviews,
                Discussions = allThreads,
            };
        }

        public Task<MapLocationGet?> GetLocationWithCategoryAsync(int id)
        {
            return _mapRepository.GetLocationWithCategoryAsync(id: id);
        }

        public async Task<bool?> HasCurrentUserReviewedLocationAsync(int locationId)
        {
            return !_userContextService.IsAuthenticated
                ? null
                : await _reviewRepository.HasUserReviewedLocationAsync(userId: _userContextService.UserIdRequired, locationId: locationId);
        }

        /// <summary>
        /// Retrieves data for the Add Location page, including categories.
        /// </summary>
        public async Task<AddLocationPageViewModel> GetAddLocationPageViewModelAsync()
        {
            IEnumerable<CategoryGet> categories = await GetAllCategoriesAsync();
            return new AddLocationPageViewModel
            {
                Categories = categories
            };
        }

        /// <summary>
        /// Retrieves data for the Edit Location page, including current values and categories.
        /// </summary>
        public async Task<EditLocationPageViewModel> GetEditLocationPageViewModelAsync(int id)
        {
            MapLocationGet? existing = await GetLocationByIdAsync(id: id) ?? throw new KeyNotFoundException($"Location {id} not found");
            if (!_userContextService.IsAuthenticated || !_userContextService.IsAdminRequired)
                throw new UnauthorizedAccessException("User is not allowed to edit this location");

            IEnumerable<CategoryGet> categories = await GetAllCategoriesAsync();
            MapLocationUpdate updateModel = _mapper.Map<MapLocationUpdate>(source: existing);
            return new EditLocationPageViewModel
            {
                UpdateModel = updateModel,
                Categories = categories
            };
        }

        private void EnsureUserCanEditLocation(int locationAuthorId)
        {
            _userContextService.EnsureAuthenticated();
            if (locationAuthorId != _userContextService.UserIdRequired && !_userContextService.IsAdminRequired)
                throw new UnauthorizedAccessException("User is not allowed to update this location");
        }
    }
}
