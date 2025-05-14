namespace MapHive.Controllers
{
    using MapHive.Models.ViewModels;
    using MapHive.Services;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    public class ReviewController(IReviewService reviewService, IUserContextService userContextService) : Controller
    {
        private readonly IReviewService _reviewService = reviewService;
        private readonly IUserContextService _userContextService = userContextService;

        [Authorize]
        public async Task<IActionResult> Create(int id)
        {
            ReviewViewModel model = await _reviewService.GetCreateModelAsync(locationId: id);
            return View(model: model);
        }

        // POST: Review/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create(ReviewViewModel reviewViewModel)
        {
            if (ModelState.IsValid)
            {
                int accountId = _userContextService.AccountIdRequired;
                _ = await _reviewService.CreateReviewAsync(model: reviewViewModel, accountId: accountId);
                return RedirectToAction(actionName: "Details", controllerName: "Map", routeValues: new { id = reviewViewModel.LocationId });
            }
            return View(model: reviewViewModel);
        }

        // GET: Review/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            int accountId = _userContextService.AccountIdRequired;
            ReviewViewModel model = await _reviewService.GetEditModelAsync(reviewId: id, accountId: accountId);
            return View(model: model);
        }

        // POST: Review/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, ReviewViewModel reviewViewModel)
        {
            if (ModelState.IsValid)
            {
                int accountId = _userContextService.AccountIdRequired;
                await _reviewService.EditReviewAsync(id: id, model: reviewViewModel, accountId: accountId);
                return RedirectToAction(actionName: "Details", controllerName: "Map", routeValues: new { id = reviewViewModel.LocationId });
            }
            return View(model: reviewViewModel);
        }

        // POST: Review/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            int accountId = _userContextService.AccountIdRequired;
            int locationId = await _reviewService.DeleteReviewAsync(id: id, accountId: accountId);
            return RedirectToAction(actionName: "Details", controllerName: "Map", routeValues: new { id = locationId });
        }
    }
}