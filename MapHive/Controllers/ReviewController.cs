namespace MapHive.Controllers;

using MapHive.Models.PageModels;
using MapHive.Repositories;
using MapHive.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

public class ReviewController(
    IReviewService _reviewService,
    IMapLocationRepository _mapRepository
) : Controller
{
    [HttpGet("Review/Create/{id:int:required}")]
    [Authorize]
    public async Task<IActionResult> Create(int id)
    {
        return View(new ReviewUpdatePageModel
        {
            LocationName = (await _mapRepository.GetLocationByIdOrThrowAsync(id: id)).Name,
            Rating = 0,
            ReviewText = string.Empty,
            IsAnonymous = false,
        });
    }

    // POST: Review/Create
    [HttpPost("Review/Create/{id:int:required}")]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Create(int id, ReviewUpdatePageModel reviewPageModel)
    {
        if (!ModelState.IsValid)
            return View(model: reviewPageModel);
        _ = await _reviewService.CreateReviewAsync(locationId: id, model: reviewPageModel);
        return RedirectToAction(actionName: "Details", controllerName: "Map", routeValues: new { id });
    }

    [HttpGet("Review/Edit/{id:int:required}")]
    [Authorize]
    public async Task<IActionResult> Edit(int id)
    {
        ReviewUpdatePageModel model = await _reviewService.GetEditModelAsync(reviewId: id);
        return View(model: model);
    }

    [HttpPost("Review/Edit/{id:int:required}")]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Edit(int id, ReviewUpdatePageModel reviewPageModel)
    {
        if (!ModelState.IsValid)
        {
            return View(model: reviewPageModel);
        }
        int locationId = await _reviewService.EditReviewAsync(id: id, model: reviewPageModel);
        return RedirectToAction(actionName: "Details", controllerName: "Map", routeValues: new { id = locationId });

    }

    [HttpPost("Review/Delete/{id:int:required}")]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        int locationId = await _reviewService.DeleteReviewAsync(id: id);
        return RedirectToAction(actionName: "Details", controllerName: "Map", routeValues: new { id = locationId });
    }
}
