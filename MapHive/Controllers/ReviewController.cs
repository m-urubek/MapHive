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
                int userId = _userContextService.UserId;
                _ = await _reviewService.CreateReviewAsync(model: reviewViewModel, userId: userId);
                return RedirectToAction(actionName: "Details", controllerName: "Map", routeValues: new { id = reviewViewModel.LocationId });
            }
            return View(model: reviewViewModel);
        }

        // GET: Review/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            int userId = _userContextService.UserId;
            ReviewViewModel model = await _reviewService.GetEditModelAsync(reviewId: id, userId: userId);
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
                int userId = _userContextService.UserId;
                await _reviewService.EditReviewAsync(id: id, model: reviewViewModel, userId: userId);
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
            int userId = _userContextService.UserId;
            bool isAdmin = User.IsInRole(role: "Admin");
            int locationId = await _reviewService.DeleteReviewAsync(id: id, userId: userId, isAdmin: isAdmin);
            return RedirectToAction(actionName: "Details", controllerName: "Map", routeValues: new { id = locationId });
        }
    }
}