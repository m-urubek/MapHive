using MapHive.Models;
using MapHive.Models.Exceptions;
using MapHive.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MapHive.Controllers
{
    public class MapController : Controller
    {
        private readonly IMapLocationRepository _repository;
        private readonly IUserRepository _userRepository;
        private readonly IReviewRepository _reviewRepository;
        private readonly IDiscussionRepository _discussionRepository;

        public MapController(
            IMapLocationRepository repository, 
            IUserRepository userRepository,
            IReviewRepository reviewRepository,
            IDiscussionRepository discussionRepository)
        {
            this._repository = repository;
            this._userRepository = userRepository;
            this._reviewRepository = reviewRepository;
            this._discussionRepository = discussionRepository;
        }

        // GET: Map
        public async Task<IActionResult> Index()
        {
            IEnumerable<MapLocation> locations = await this._repository.GetAllLocationsAsync();
            return this.View(locations);
        }

        // GET: Map/Add
        [Authorize] // Only authenticated users can access this action
        public IActionResult Add()
        {
            return this.View();
        }

        // POST: Map/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize] // Only authenticated users can create places
        public async Task<IActionResult> Add(MapLocation location)
        {
            if (this.ModelState.IsValid)
            {
                // Set the current user as the creator
                string? userId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int id))
                {
                    throw new OrangeUserException("User ID not found in claims");
                }
                location.UserId = id;

                _ = await this._repository.AddLocationAsync(location);
                return this.RedirectToAction(nameof(Index));
            }
            return this.View(location);
        }

        // GET: Map/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            MapLocation location = await this._repository.GetLocationByIdAsync(id);
            if (location == null)
            {
                return this.NotFound();
            }

            // Only allow the creator or admin to edit
            string? userId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            bool isAdmin = this.User.IsInRole("Admin");

            return !isAdmin && (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int currentUserId) || location.UserId != currentUserId)
                ? this.Forbid()
                : this.View(location);
        }

        // POST: Map/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, MapLocation location)
        {
            if (id != location.Id)
            {
                return this.NotFound();
            }

            MapLocation existingLocation = await this._repository.GetLocationByIdAsync(id);
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

            if (this.ModelState.IsValid)
            {
                _ = await this._repository.UpdateLocationAsync(location);
                return this.RedirectToAction(nameof(Index));
            }
            return this.View(location);
        }

        // GET: Map/Delete/5
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            MapLocation location = await this._repository.GetLocationByIdAsync(id);
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
            MapLocation location = await this._repository.GetLocationByIdAsync(id);
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

            _ = await this._repository.DeleteLocationAsync(id);
            return this.RedirectToAction(nameof(Index));
        }

        // GET: Map/Details/5
        public async Task<IActionResult> Details(int id)
        {
            MapLocation location = await this._repository.GetLocationByIdAsync(id);
            if (location == null)
            {
                return this.NotFound();
            }

            // Get the author's username if not anonymous
            if (!location.IsAnonymous)
            {
                // We'll need the username for display, so use ViewBag to pass it
                User? user = this._userRepository.GetUserById(location.UserId);
                this.ViewBag.AuthorUsername = user?.Username ?? "Unknown";
            }
            else
            {
                this.ViewBag.AuthorUsername = "Anonymous";
            }

            // Get reviews for this location
            var reviews = await this._reviewRepository.GetReviewsByLocationIdAsync(id);
            this.ViewBag.Reviews = reviews;
            
            // Get average rating
            double averageRating = await this._reviewRepository.GetAverageRatingForLocationAsync(id);
            this.ViewBag.AverageRating = averageRating;
            
            // Get review count
            int reviewCount = await this._reviewRepository.GetReviewCountForLocationAsync(id);
            this.ViewBag.ReviewCount = reviewCount;
            
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
            
            // Get discussion threads for this location
            var discussionThreads = await this._discussionRepository.GetAllDiscussionThreadsByLocationIdAsync(id);
            this.ViewBag.DiscussionThreads = discussionThreads;
            
            // Calculate the count of regular discussion threads (excluding review threads)
            this.ViewBag.RegularDiscussionCount = discussionThreads.Count(t => !t.IsReviewThread);

            return this.View(location);
        }

        // API endpoint to get all locations as JSON
        [HttpGet]
        public async Task<IActionResult> GetLocations()
        {
            IEnumerable<MapLocation> locations = await this._repository.GetAllLocationsAsync();
            return this.Json(locations);
        }
    }
}