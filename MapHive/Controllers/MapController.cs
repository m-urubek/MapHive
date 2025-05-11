namespace MapHive.Controllers
{
    using AutoMapper;
    using MapHive.Models.RepositoryModels;
    using MapHive.Models.ViewModels;
    using MapHive.Services;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    public class MapController(IMapService mapService, IUserContextService userContextService) : Controller
    {
        private readonly IMapService _mapService = mapService;
        private readonly IUserContextService _userContextService = userContextService;

        // GET: Map
        public async Task<IActionResult> Index()
        {
            IEnumerable<MapLocationGet> locations = await _mapService.GetAllLocationsAsync();
            return View(model: locations);
        }

        // GET: Map/Add
        [Authorize]
        public async Task<IActionResult> Add()
        {
            // Prepare Add page data
            int userId = _userContextService.UserId;
            AddLocationPageViewModel vm = await _mapService.GetAddLocationPageViewModelAsync(currentUserId: userId);
            return View(model: vm);
        }

        // POST: Map/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Add(MapLocationCreate mapLocationCreate)
        {
            if (!ModelState.IsValid)
            {
                // Re-populate categories on validation failure
                int userId = _userContextService.UserId;
                AddLocationPageViewModel fallback = await _mapService.GetAddLocationPageViewModelAsync(currentUserId: userId);
                return View(model: fallback);
            }
            _ = await _mapService.AddLocationAsync(mapLocationCreate: mapLocationCreate);
            return RedirectToAction(actionName: nameof(Index));
        }

        // GET: Map/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            int userId = _userContextService.UserId;
            bool isAdmin = User.IsInRole(role: "Admin");
            try
            {
                EditLocationPageViewModel vm = await _mapService.GetEditLocationPageViewModelAsync(id: id, currentUserId: userId, isAdmin: isAdmin);
                return View(model: vm);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        // POST: Map/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(EditLocationPageViewModel editLocationPageViewModel)
        {
            if (!ModelState.IsValid)
            {
                // Re-populate categories on validation failure
                int userId = _userContextService.UserId;
                bool isAdmin = User.IsInRole(role: "Admin");
                EditLocationPageViewModel fallback = await _mapService.GetEditLocationPageViewModelAsync(id: editLocationPageViewModel.UpdateModel.Id, currentUserId: userId, isAdmin: isAdmin);
                fallback.UpdateModel = editLocationPageViewModel.UpdateModel;
                return View(model: fallback);
            }
            _ = await _mapService.UpdateLocationAsync(id: editLocationPageViewModel.UpdateModel.Id, updateDto: editLocationPageViewModel.UpdateModel, currentUserId: _userContextService.UserId, isAdmin: User.IsInRole(role: "Admin"));
            return RedirectToAction(actionName: nameof(Index));
        }

        // GET: Map/Delete/5
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            int userId = _userContextService.UserId;
            bool isAdmin = User.IsInRole(role: "Admin");
            try
            {
                MapLocationGet location = await _mapService.GetLocationForDeleteAsync(id: id, currentUserId: userId, isAdmin: isAdmin);
                return View(model: location);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        // POST: Map/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            int userId = _userContextService.UserId;
            bool isAdmin = User.IsInRole(role: "Admin");
            try
            {
                _ = await _mapService.DeleteLocationAsync(id: id, currentUserId: userId, isAdmin: isAdmin);
                return RedirectToAction(actionName: nameof(Index));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        // GET: Map/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                // Retrieve all details via service
                MapLocationViewModel viewModel = await _mapService.GetLocationDetailsAsync(id: id, currentUserId: _userContextService.UserId);
                // Determine if current user has already reviewed
                ViewBag.HasReviewed = await _mapService.HasUserReviewedLocationAsync(userId: _userContextService.UserId, locationId: id);
                ViewBag.AuthorUsername = viewModel.AuthorName;
                ViewBag.AverageRating = viewModel.AverageRating;
                ViewBag.ReviewCount = viewModel.ReviewCount;
                ViewBag.RegularDiscussionCount = viewModel.RegularDiscussionCount;
                return View(model: viewModel);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        // API endpoint to get all locations as JSON
        [HttpGet]
        public async Task<IActionResult> GetLocations()
        {
            IEnumerable<MapLocationGet> locations = await _mapService.GetAllLocationsAsync();
            return Json(data: locations);
        }
    }
}
