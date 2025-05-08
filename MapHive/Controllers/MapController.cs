using AutoMapper;
using MapHive.Models.RepositoryModels;
using MapHive.Models.ViewModels;
using MapHive.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MapHive.Controllers
{
    public class MapController : Controller
    {
        private readonly IMapService _mapService;
        private readonly IUserContextService _userContextService;
        private readonly IMapper _mapper;

        public MapController(IMapService mapService, IUserContextService userContextService, IMapper mapper)
        {
            this._mapService = mapService;
            this._userContextService = userContextService;
            this._mapper = mapper;
        }

        // GET: Map
        public async Task<IActionResult> Index()
        {
            IEnumerable<MapLocationGet> locations = await this._mapService.GetAllLocationsAsync();
            return this.View(locations);
        }

        // GET: Map/Add
        [Authorize]
        public async Task<IActionResult> Add()
        {
            // Prepare Add page data
            int userId = this._userContextService.UserId ?? 0;
            AddLocationPageViewModel vm = await this._mapService.GetAddLocationPageViewModelAsync(userId);
            return this.View(vm);
        }

        // POST: Map/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Add(AddLocationPageViewModel addLocationPageViewModel)
        {
            if (!this.ModelState.IsValid)
            {
                // Re-populate categories on validation failure
                int userId = this._userContextService.UserId ?? 0;
                AddLocationPageViewModel fallback = await this._mapService.GetAddLocationPageViewModelAsync(userId);
                fallback.CreateModel = addLocationPageViewModel.CreateModel;
                return this.View(fallback);
            }
            _ = await this._mapService.AddLocationAsync(addLocationPageViewModel.CreateModel, addLocationPageViewModel.CreateModel.UserId);
            return this.RedirectToAction(nameof(Index));
        }

        // GET: Map/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            int userId = this._userContextService.UserId ?? 0;
            bool isAdmin = this.User.IsInRole("Admin");
            try
            {
                EditLocationPageViewModel vm = await this._mapService.GetEditLocationPageViewModelAsync(id, userId, isAdmin);
                return this.View(vm);
            }
            catch (KeyNotFoundException)
            {
                return this.NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return this.Forbid();
            }
        }

        // POST: Map/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(EditLocationPageViewModel editLocationPageViewModel)
        {
            if (!this.ModelState.IsValid)
            {
                // Re-populate categories on validation failure
                int userId = this._userContextService.UserId ?? 0;
                bool isAdmin = this.User.IsInRole("Admin");
                EditLocationPageViewModel fallback = await this._mapService.GetEditLocationPageViewModelAsync(editLocationPageViewModel.UpdateModel.Id, userId, isAdmin);
                fallback.UpdateModel = editLocationPageViewModel.UpdateModel;
                return this.View(fallback);
            }
            _ = await this._mapService.UpdateLocationAsync(editLocationPageViewModel.UpdateModel.Id, editLocationPageViewModel.UpdateModel, this._userContextService.UserId ?? 0, this.User.IsInRole("Admin"));
            return this.RedirectToAction(nameof(Index));
        }

        // GET: Map/Delete/5
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            int userId = this._userContextService.UserId ?? 0;
            bool isAdmin = this.User.IsInRole("Admin");
            try
            {
                MapLocationGet location = await this._mapService.GetLocationForDeleteAsync(id, userId, isAdmin);
                return this.View(location);
            }
            catch (KeyNotFoundException)
            {
                return this.NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return this.Forbid();
            }
        }

        // POST: Map/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            int userId = this._userContextService.UserId ?? 0;
            bool isAdmin = this.User.IsInRole("Admin");
            try
            {
                _ = await this._mapService.DeleteLocationAsync(id, userId, isAdmin);
                return this.RedirectToAction(nameof(Index));
            }
            catch (KeyNotFoundException)
            {
                return this.NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return this.Forbid();
            }
        }

        // GET: Map/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                // Retrieve all details via service
                MapLocationViewModel viewModel = await this._mapService.GetLocationDetailsAsync(id, this._userContextService.UserId);
                // Determine if current user has already reviewed
                bool hasReviewed = false;
                if (this._userContextService.UserId.HasValue)
                {
                    hasReviewed = await this._mapService.HasUserReviewedLocationAsync(this._userContextService.UserId.Value, id);
                }
                this.ViewBag.HasReviewed = hasReviewed;
                this.ViewBag.AuthorUsername = viewModel.AuthorName;
                this.ViewBag.AverageRating = viewModel.AverageRating;
                this.ViewBag.ReviewCount = viewModel.ReviewCount;
                this.ViewBag.RegularDiscussionCount = viewModel.RegularDiscussionCount;
                return this.View(viewModel);
            }
            catch (KeyNotFoundException)
            {
                return this.NotFound();
            }
        }

        // API endpoint to get all locations as JSON
        [HttpGet]
        public async Task<IActionResult> GetLocations()
        {
            IEnumerable<MapLocationGet> locations = await this._mapService.GetAllLocationsAsync();
            return this.Json(locations);
        }
    }
}