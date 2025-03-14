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

        public MapController(IMapLocationRepository repository, IUserRepository userRepository)
        {
            this._repository = repository;
            this._userRepository = userRepository;
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