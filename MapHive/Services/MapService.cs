using AutoMapper;
using MapHive.Models.RepositoryModels;
using MapHive.Models.ViewModels;
using MapHive.Repositories;

namespace MapHive.Services
{
    public class MapService : IMapService
    {
        private readonly IMapLocationRepository _mapRepo;
        private readonly IDiscussionRepository _discussionRepo;
        private readonly IReviewRepository _reviewRepo;
        private readonly IUserRepository _userRepo;
        private readonly IMapper _mapper;

        public MapService(
            IMapLocationRepository mapRepo,
            IDiscussionRepository discussionRepo,
            IReviewRepository reviewRepo,
            IUserRepository userRepo,
            IMapper mapper)
        {
            this._mapRepo = mapRepo;
            this._discussionRepo = discussionRepo;
            this._reviewRepo = reviewRepo;
            this._userRepo = userRepo;
            this._mapper = mapper;
        }

        public Task<IEnumerable<MapLocationGet>> GetAllLocationsAsync()
        {
            return this._mapRepo.GetAllLocationsAsync();
        }

        public Task<IEnumerable<CategoryGet>> GetAllCategoriesAsync()
        {
            return this._mapRepo.GetAllCategoriesAsync();
        }

        public Task<MapLocationGet?> GetLocationByIdAsync(int id)
        {
            return this._mapRepo.GetLocationByIdAsync(id);
        }

        public async Task<MapLocationGet> AddLocationAsync(MapLocationCreate createDto, int userId)
        {
            createDto.UserId = userId;
            return await this._mapRepo.AddLocationAsync(createDto);
        }

        public async Task<MapLocationGet?> UpdateLocationAsync(int id, MapLocationUpdate updateDto, int currentUserId, bool isAdmin)
        {
            MapLocationGet? existing = await this._mapRepo.GetLocationByIdAsync(id);
            return existing == null
                ? null
                : !isAdmin && existing.UserId != currentUserId
                ? throw new UnauthorizedAccessException("User is not allowed to update this location")
                : await this._mapRepo.UpdateLocationAsync(updateDto);
        }

        public async Task<bool> DeleteLocationAsync(int id, int currentUserId, bool isAdmin)
        {
            MapLocationGet? existing = await this._mapRepo.GetLocationByIdAsync(id);
            return existing != null && (!isAdmin && existing.UserId != currentUserId
                ? throw new UnauthorizedAccessException("User is not allowed to delete this location")
                : await this._mapRepo.DeleteLocationAsync(id));
        }

        public async Task<MapLocationViewModel> GetLocationDetailsAsync(int id, int? currentUserId)
        {
            MapLocationGet? location = await this._mapRepo.GetLocationWithCategoryAsync(id);
            if (location == null)
            {
                throw new KeyNotFoundException($"Location with ID {id} not found");
            }

            MapLocationViewModel viewModel = this._mapper.Map<MapLocationViewModel>(location);

            if (!location.IsAnonymous)
            {
                UserGet? user = await this._userRepo.GetUserByIdAsync(location.UserId);
                viewModel.AuthorName = user?.Username ?? "Unknown";
            }
            else
            {
                viewModel.AuthorName = "Anonymous";
            }

            List<ReviewGet> reviews = (await this._reviewRepo.GetReviewsByLocationIdAsync(id)).ToList();
            viewModel.Reviews = reviews;
            viewModel.AverageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
            viewModel.ReviewCount = reviews.Count;

            List<DiscussionThreadGet> discussions = (await this._discussionRepo.GetDiscussionThreadsByLocationIdAsync(id))
                                  .Where(d => !d.IsReviewThread)
                                  .ToList();
            viewModel.Discussions = discussions;
            viewModel.RegularDiscussionCount = discussions.Count;

            return viewModel;
        }

        public Task<MapLocationGet?> GetLocationWithCategoryAsync(int id)
        {
            return this._mapRepo.GetLocationWithCategoryAsync(id);
        }

        public Task<bool> HasUserReviewedLocationAsync(int userId, int locationId)
        {
            return this._reviewRepo.HasUserReviewedLocationAsync(userId, locationId);
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

            IEnumerable<CategoryGet> categories = await this.GetAllCategoriesAsync();
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
            MapLocationGet? existing = await this.GetLocationByIdAsync(id);
            if (existing == null)
            {
                throw new KeyNotFoundException($"Location {id} not found");
            }

            if (!isAdmin && existing.UserId != currentUserId)
            {
                throw new UnauthorizedAccessException("User is not allowed to edit this location");
            }

            IEnumerable<CategoryGet> categories = await this.GetAllCategoriesAsync();
            MapLocationUpdate updateModel = this._mapper.Map<MapLocationUpdate>(existing);
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
            MapLocationGet? existing = await this.GetLocationWithCategoryAsync(id);
            return existing == null
                ? throw new KeyNotFoundException($"Location {id} not found")
                : !isAdmin && existing.UserId != currentUserId
                ? throw new UnauthorizedAccessException("User is not allowed to delete this location")
                : existing;
        }
    }
}