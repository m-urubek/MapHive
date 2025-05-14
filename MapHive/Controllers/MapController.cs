namespace MapHive.Controllers
{
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
            AddLocationPageViewModel vm = await _mapService.GetAddLocationPageViewModelAsync();
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
                int accountId = _userContextService.AccountIdRequired;
                AddLocationPageViewModel fallback = await _mapService.GetAddLocationPageViewModelAsync();
                return View(model: fallback);
            }
            _ = await _mapService.AddLocationAsync(mapLocationCreate: mapLocationCreate);
            return RedirectToAction(actionName: nameof(Index));
        }

        // GET: Map/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                EditLocationPageViewModel vm = await _mapService.GetEditLocationPageViewModelAsync(id: id);
                return View(model: vm);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
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
                EditLocationPageViewModel fallback = await _mapService.GetEditLocationPageViewModelAsync(id: editLocationPageViewModel.UpdateModel.Id);
                fallback.UpdateModel = editLocationPageViewModel.UpdateModel;
                return View(model: fallback);
            }
            _ = await _mapService.UpdateLocationAsync(id: editLocationPageViewModel.UpdateModel.Id, updateDto: editLocationPageViewModel.UpdateModel);
            return RedirectToAction(actionName: nameof(Index));
        }

        // GET: Map/Delete/5
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                MapLocationGet location = await _mapService.GetLocationByIdOrThrowAsync(id: id);
                return View(model: location);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        // POST: Map/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                _ = await _mapService.DeleteLocationAsync(id: id);
                return RedirectToAction(actionName: nameof(Index));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        // GET: Map/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                // Retrieve all details via service
                MapLocationViewModel viewModel = await _mapService.GetLocationDetailsAsync(id: id);
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
