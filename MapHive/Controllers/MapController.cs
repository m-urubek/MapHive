using AutoMapper;
using MapHive.Models.Exceptions;
using MapHive.Models.RepositoryModels;
using MapHive.Models.ViewModels;
using MapHive.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MapHive.Controllers
{
    public class MapController : Controller
    {
        private readonly IMapLocationRepository _mapLocationRepository;
        private readonly IDiscussionRepository _discussionRepository;
        private readonly IReviewRepository _reviewRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        public MapController(
            IMapLocationRepository mapLocationRepository,
            IDiscussionRepository discussionRepository,
            IReviewRepository reviewRepository,
            IUserRepository userRepository,
            IMapper mapper)
        {
            this._mapLocationRepository = mapLocationRepository;
            this._discussionRepository = discussionRepository;
            this._reviewRepository = reviewRepository;
            this._userRepository = userRepository;
            this._mapper = mapper;
        }

        // GET: Map
        public async Task<IActionResult> Index()
        {
            IEnumerable<MapLocationGet> locations = await this._mapLocationRepository.GetAllLocationsAsync();
            return this.View(locations);
        }

        // GET: Map/Add
        [Authorize] // Only authenticated users can access this action
        public async Task<IActionResult> Add()
        {
            // Get all categories for dropdown
            IEnumerable<CategoryGet> categories = await this._mapLocationRepository.GetAllCategoriesAsync();
            this.ViewBag.Categories = categories;

            return this.View();
        }

        // POST: Map/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize] // Only authenticated users can create places
        public async Task<IActionResult> Add(MapLocationCreate location)
        {
            // Get all categories for dropdown, fetch before model state check
            IEnumerable<CategoryGet> categories = await this._mapLocationRepository.GetAllCategoriesAsync();

            if (this.ModelState.IsValid)
            {
                // Set the current user as the creator
                string? userId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int id))
                {
                    throw new OrangeUserException("UserLogin ID not found in claims");
                }
                location.UserId = id;

                _ = await this._mapLocationRepository.AddLocationAsync(location);
                return this.RedirectToAction(nameof(Index));
            }

            // If we get here, there was an error, so assign pre-fetched categories to ViewBag
            this.ViewBag.Categories = categories;

            return this.View(location);
        }

        // GET: Map/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            MapLocationGet? locationGet = await this._mapLocationRepository.GetLocationByIdAsync(id);
            if (locationGet == null)
            {
                return this.NotFound();
            }

            // Only allow the creator or admin to edit
            string? userId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            bool isAdmin = this.User.IsInRole("Admin");

            if (!isAdmin && (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int currentUserId) || locationGet.UserId != currentUserId))
            {
                return this.Forbid();
            }

            // Get all categories for dropdown
            IEnumerable<CategoryGet> categories = await this._mapLocationRepository.GetAllCategoriesAsync();
            this.ViewBag.Categories = categories;

            // Map to update model
            MapLocationUpdate model = this._mapper.Map<MapLocationUpdate>(locationGet);
            return this.View(model);
        }

        // POST: Map/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, MapLocationUpdate location)
        {
            if (id != location.Id)
            {
                return this.NotFound();
            }

            MapLocationGet existingLocation = await this._mapLocationRepository.GetLocationByIdAsync(id);
            if (existingLocation == null)
            {
                return this.NotFound();
            }

            // Only allow the creator or admin to edit
            string? userId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            bool isAdmin = this.User.IsInRole("Admin");

            if (!isAdmin && (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int currentUserId) || existingLocation.UserId != currentUserId))
            {
                return this.Forbid();
            }

            // Get all categories for dropdown, fetch before model state check
            IEnumerable<CategoryGet> categories = await this._mapLocationRepository.GetAllCategoriesAsync();

            if (this.ModelState.IsValid)
            {
                _ = await this._mapLocationRepository.UpdateLocationAsync(location);
                return this.RedirectToAction(nameof(Index));
            }

            // If we get here, there was an error, so assign pre-fetched categories to ViewBag
            this.ViewBag.Categories = categories;

            return this.View(location);
        }

        // GET: Map/Delete/5
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            MapLocationGet location = await this._mapLocationRepository.GetLocationWithCategoryAsync(id);
            if (location == null)
            {
                return this.NotFound();
            }

            // Only allow the creator or admin to delete
            string? userId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            bool isAdmin = this.User.IsInRole("Admin");

            return !isAdmin && (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int currentUserId) || location.UserId != currentUserId)
                ? this.Forbid()
                : this.View(location);
        }

        // POST: Map/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            MapLocationGet location = await this._mapLocationRepository.GetLocationByIdAsync(id);
            if (location == null)
            {
                return this.NotFound();
            }

            // Only allow the creator or admin to delete
            string? userId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            bool isAdmin = this.User.IsInRole("Admin");

            if (!isAdmin && (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int currentUserId) || location.UserId != currentUserId))
            {
                return this.Forbid();
            }

            _ = await this._mapLocationRepository.DeleteLocationAsync(id);
            return this.RedirectToAction(nameof(Index));
        }

        // GET: Map/Details/5
        public async Task<IActionResult> Details(int id)
        {
            MapLocationGet location = await this._mapLocationRepository.GetLocationWithCategoryAsync(id);
            if (location == null)
            {
                return this.NotFound();
            }

            // Use AutoMapper to map repository model to view model
            MapLocationViewModel viewModel = this._mapper.Map<MapLocationViewModel>(location);

            // Get the author's username if not anonymous
            if (!location.IsAnonymous)
            {
                UserGet? userGet = await this._userRepository.GetUserByIdAsync(location.UserId);
                viewModel.AuthorName = userGet?.Username ?? "Unknown";
            }
            else
            {
                viewModel.AuthorName = "Anonymous";
            }

            // Get reviews for this location
            IEnumerable<ReviewGet> reviews = await this._reviewRepository.GetReviewsByLocationIdAsync(id);
            viewModel.Reviews = reviews.ToList();

            // Calculate average rating
            if (reviews.Any())
            {
                viewModel.AverageRating = reviews.Average(r => r.Rating);
                viewModel.ReviewCount = reviews.Count();
            }
            else
            {
                viewModel.AverageRating = 0;
                viewModel.ReviewCount = 0;
            }

            // Check if the current user has already reviewed this location
            bool hasReviewed = false;
            if (this.User.Identity?.IsAuthenticated == true)
            {
                string? userId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out int currentUserId))
                {
                    hasReviewed = await this._reviewRepository.HasUserReviewedLocationAsync(currentUserId, id);
                }
            }
            this.ViewBag.HasReviewed = hasReviewed;

            // Get regular discussions (non-review threads)
            IEnumerable<DiscussionThreadGet> discussions = await this._discussionRepository.GetDiscussionThreadsByLocationIdAsync(id);
            List<DiscussionThreadGet> regularDiscussions = discussions.Where(d => !d.IsReviewThread).ToList();
            viewModel.Discussions = regularDiscussions;
            viewModel.RegularDiscussionCount = regularDiscussions.Count;

            return this.View(viewModel);
        }

        // API endpoint to get all locations as JSON
        [HttpGet]
        public async Task<IActionResult> GetLocations()
        {
            IEnumerable<MapLocationGet> locations = await this._mapLocationRepository.GetAllLocationsAsync();
            return this.Json(locations);
        }
    }
}