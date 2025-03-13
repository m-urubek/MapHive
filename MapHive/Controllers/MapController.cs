using MapHive.Models;
using MapHive.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace MapHive.Controllers
{
    public class MapController : Controller
    {
        private readonly IMapLocationRepository _repository;

        public MapController(IMapLocationRepository repository)
        {
            this._repository = repository;
        }

        // GET: Map
        public async Task<IActionResult> Index()
        {
            IEnumerable<MapLocation> locations = await this._repository.GetAllLocationsAsync();
            return this.View(locations);
        }

        // GET: Map/Add
        public IActionResult Add()
        {
            return this.View();
        }

        // POST: Map/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(MapLocation location)
        {
            if (this.ModelState.IsValid)
            {
                _ = await this._repository.AddLocationAsync(location);
                return this.RedirectToAction(nameof(Index));
            }
            return this.View(location);
        }

        // GET: Map/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            MapLocation location = await this._repository.GetLocationByIdAsync(id);
            return location == null ? this.NotFound() : this.View(location);
        }

        // POST: Map/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MapLocation location)
        {
            if (id != location.Id)
            {
                return this.NotFound();
            }

            if (this.ModelState.IsValid)
            {
                _ = await this._repository.UpdateLocationAsync(location);
                return this.RedirectToAction(nameof(Index));
            }
            return this.View(location);
        }

        // GET: Map/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            MapLocation location = await this._repository.GetLocationByIdAsync(id);
            return location == null ? this.NotFound() : this.View(location);
        }

        // POST: Map/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            _ = await this._repository.DeleteLocationAsync(id);
            return this.RedirectToAction(nameof(Index));
        }

        // GET: Map/Details/5
        public async Task<IActionResult> Details(int id)
        {
            MapLocation location = await this._repository.GetLocationByIdAsync(id);
            return location == null ? this.NotFound() : this.View(location);
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