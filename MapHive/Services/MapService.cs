namespace MapHive.Services
{
    using AutoMapper;
    using MapHive.Models.RepositoryModels;
    using MapHive.Models.ViewModels;
    using MapHive.Repositories;

    public class MapService(
        IMapLocationRepository mapRepository,
        IDiscussionRepository discussionRepository,
        IReviewRepository reviewRepository,
        IUserRepository userRepository,
        IMapper mapper) : IMapService
    {
        private readonly IMapLocationRepository _mapRepository = mapRepository;
        private readonly IDiscussionRepository _discussionRepository = discussionRepository;
        private readonly IReviewRepository _reviewRepository = reviewRepository;
        private readonly IUserRepository _userRepository = userRepository;
        private readonly IMapper _mapper = mapper;

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

        public async Task<MapLocationGet> AddLocationAsync(MapLocationCreate createDto, int userId)
        {
            createDto.UserId = userId;
            return await _mapRepository.AddLocationAsync(location: createDto);
        }

        public async Task<MapLocationGet?> UpdateLocationAsync(int id, MapLocationUpdate updateDto, int currentUserId, bool isAdmin)
        {
            MapLocationGet? existing = await _mapRepository.GetLocationByIdAsync(id: id);
            return existing == null
                ? null
                : !isAdmin && existing.UserId != currentUserId
                ? throw new UnauthorizedAccessException("User is not allowed to update this location")
                : await _mapRepository.UpdateLocationAsync(location: updateDto);
        }

        public async Task<bool> DeleteLocationAsync(int id, int currentUserId, bool isAdmin)
        {
            MapLocationGet? existing = await _mapRepository.GetLocationByIdAsync(id: id);
            return existing != null && (!isAdmin && existing.UserId != currentUserId
                ? throw new UnauthorizedAccessException("User is not allowed to delete this location")
                : await _mapRepository.DeleteLocationAsync(id: id));
        }

        public async Task<MapLocationViewModel> GetLocationDetailsAsync(int id, int? currentUserId)
        {
            MapLocationGet? location = await _mapRepository.GetLocationWithCategoryAsync(id: id);
            if (location == null)
            {
                throw new KeyNotFoundException($"Location with ID {id} not found");
            }

            MapLocationViewModel viewModel = _mapper.Map<MapLocationViewModel>(source: location);

            if (!location.IsAnonymous)
            {
                UserGet? user = await _userRepository.GetUserByIdAsync(id: location.UserId);
                viewModel.AuthorName = user?.Username ?? "Unknown";
            }
            else
            {
                viewModel.AuthorName = "Anonymous";
            }

            List<ReviewGet> reviews = (await _reviewRepository.GetReviewsByLocationIdAsync(locationId: id)).ToList();
            viewModel.Reviews = reviews;
            viewModel.AverageRating = reviews.Count != 0 ? reviews.Average(selector: r => r.Rating) : 0;
            viewModel.ReviewCount = reviews.Count;

            List<DiscussionThreadGet> discussions = (await _discussionRepository.GetDiscussionThreadsByLocationIdAsync(locationId: id))
                                  .Where(predicate: d => !d.IsReviewThread)
                                  .ToList();
            viewModel.Discussions = discussions;
            viewModel.RegularDiscussionCount = discussions.Count;

            return viewModel;
        }

        public Task<MapLocationGet?> GetLocationWithCategoryAsync(int id)
        {
            return _mapRepository.GetLocationWithCategoryAsync(id: id);
        }

        public Task<bool> HasUserReviewedLocationAsync(int userId, int locationId)
        {
            return _reviewRepository.HasUserReviewedLocationAsync(userId: userId, locationId: locationId);
        }

        /// <summary>
        /// Retrieves data for the Add Location page, including categories.
        /// </summary>
        public async Task<AddLocationPageViewModel> GetAddLocationPageViewModelAsync(int currentUserId)
        {
            // Only authenticated users can add
            if (currentUserId <= 0)
            {
                throw new UnauthorizedAccessException("User is not authenticated");
            }

            IEnumerable<CategoryGet> categories = await GetAllCategoriesAsync();
            return new AddLocationPageViewModel
            {
                CreateModel = new MapLocationCreate
                {
                    Name = string.Empty,
                    Description = string.Empty,
                    Latitude = 0,
                    Longitude = 0,
                    Address = string.Empty,
                    Website = string.Empty,
                    PhoneNumber = string.Empty,
                    UserId = currentUserId,
                    IsAnonymous = false,
                    CategoryId = null
                },
                Categories = categories
            };
        }

        /// <summary>
        /// Retrieves data for the Edit Location page, including current values and categories.
        /// </summary>
        public async Task<EditLocationPageViewModel> GetEditLocationPageViewModelAsync(int id, int currentUserId, bool isAdmin)
        {
            MapLocationGet? existing = await GetLocationByIdAsync(id: id);
            if (existing == null)
            {
                throw new KeyNotFoundException($"Location {id} not found");
            }

            if (!isAdmin && existing.UserId != currentUserId)
            {
                throw new UnauthorizedAccessException("User is not allowed to edit this location");
            }

            IEnumerable<CategoryGet> categories = await GetAllCategoriesAsync();
            MapLocationUpdate updateModel = _mapper.Map<MapLocationUpdate>(source: existing);
            return new EditLocationPageViewModel
            {
                UpdateModel = updateModel,
                Categories = categories
            };
        }

        /// <summary>
        /// Retrieves a location for deletion after checking authorization.
        /// </summary>
        public async Task<MapLocationGet> GetLocationForDeleteAsync(int id, int currentUserId, bool isAdmin)
        {
            MapLocationGet? existing = await GetLocationWithCategoryAsync(id: id);
            return existing == null
                ? throw new KeyNotFoundException($"Location {id} not found")
                : !isAdmin && existing.UserId != currentUserId
                ? throw new UnauthorizedAccessException("User is not allowed to delete this location")
                : existing;
        }
    }
}