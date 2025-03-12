using System.Threading.Tasks;
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
            _repository = repository;
        }

        // GET: Map
        public async Task<IActionResult> Index()
        {
            var locations = await _repository.GetAllLocationsAsync();
            return View(locations);
        }

        // GET: Map/Add
        public IActionResult Add()
        {
            return View();
        }

        // POST: Map/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(MapLocation location)
        {
            if (ModelState.IsValid)
            {
                await _repository.AddLocationAsync(location);
                return RedirectToAction(nameof(Index));
            }
            return View(location);
        }

        // GET: Map/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var location = await _repository.GetLocationByIdAsync(id);
            if (location == null)
            {
                return NotFound();
            }
            return View(location);
        }

        // POST: Map/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MapLocation location)
        {
            if (id != location.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                await _repository.UpdateLocationAsync(location);
                return RedirectToAction(nameof(Index));
            }
            return View(location);
        }

        // GET: Map/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var location = await _repository.GetLocationByIdAsync(id);
            if (location == null)
            {
                return NotFound();
            }
            return View(location);
        }

        // POST: Map/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _repository.DeleteLocationAsync(id);
            return RedirectToAction(nameof(Index));
        }

        // GET: Map/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var location = await _repository.GetLocationByIdAsync(id);
            if (location == null)
            {
                return NotFound();
            }
            return View(location);
        }

        // API endpoint to get all locations as JSON
        [HttpGet]
        public async Task<IActionResult> GetLocations()
        {
            var locations = await _repository.GetAllLocationsAsync();
            return Json(locations);
        }
    }
} 