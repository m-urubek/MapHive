namespace MapHive.Controllers;

using MapHive.Models.Data.DbTableModels;
using MapHive.Models.PageModels;
using MapHive.Repositories;
using MapHive.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

public class MapController(
    IMapLocationService _mapService,
    IUserContextService _userContextService,
    IMapLocationRepository _mapRepository) : Controller
{
    public async Task<IActionResult> Index()
    {
        IEnumerable<LocationExtended> locations = await _mapRepository.GetAllLocationsAsync();
        return View(model: locations);
    }

    [HttpGet("Map/Add")]
    [Authorize]
    public async Task<IActionResult> Add()
    {
        LocationUpdatePageModel vm = await _mapService.GetAddLocationPagePageModelAsync();
        return View(model: vm);
    }

    // POST: Map/Add
    [HttpPost("Map/Add")]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Add(LocationUpdatePageModel addLocationPageModel)
    {
        if (!ModelState.IsValid)
        {
            return View(model: addLocationPageModel);
        }
        int locationId = await _mapRepository.CreateLocationAsync(
            name: addLocationPageModel.Name!,
            description: addLocationPageModel.Description,
            latitude: addLocationPageModel.Latitude!.Value,
            longitude: addLocationPageModel.Longitude!.Value,
            address: addLocationPageModel.Address,
            website: addLocationPageModel.Website,
            phoneNumber: addLocationPageModel.PhoneNumber,
            isAnonymous: addLocationPageModel.IsAnonymous,
            categoryId: addLocationPageModel.CategoryId!.Value,
            ownerId: _userContextService.AccountIdOrThrow
        );
        return RedirectToAction(actionName: nameof(Details), new { id = locationId });
    }

    [HttpGet("Map/Edit/{id:int:required}")]
    [Authorize]
    public async Task<IActionResult> Edit(int id)
    {
        LocationUpdatePageModel vm = await _mapService.GetLocationUpdatePageModelAsync(id: id);
        return View(model: vm);
    }

    [HttpPost("Map/Edit/{id:int:required}")]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Edit(int id, LocationUpdatePageModel locationUpdatePageModel)
    {
        if (!ModelState.IsValid)
        {
            return View(model: locationUpdatePageModel);
        }
        await _mapService.UpdateLocationOrThrowAsync(id: id, locationUpdatePageModel: locationUpdatePageModel);
        return RedirectToAction(actionName: nameof(Details), new { id });
    }

    [HttpGet("Map/Delete/{id:int:required}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            LocationExtended location = await _mapService.GetLocationByIdOrThrowAsync(id: id);
            return View(model: location);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("Map/Delete/{id:int:required}")]
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

    [HttpGet("Map/Details/{id:int:required}")]
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            // Retrieve all details via service
            LocationDisplayPageModel pageModel = await _mapService.GetLocationDetailsAsync(id: id);
            return View(model: pageModel);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("Map/GetLocations")]
    public async Task<IActionResult> GetLocations()
    {
        IEnumerable<LocationExtended> locations = await _mapRepository.GetAllLocationsAsync();
        return Json(data: locations);
    }
}
