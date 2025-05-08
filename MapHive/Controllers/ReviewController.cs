using MapHive.Models.ViewModels;
using MapHive.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MapHive.Controllers
{
    public class ReviewController : Controller
    {
        private readonly IReviewService _reviewService;
        private readonly IUserContextService _userContextService;

        public ReviewController(IReviewService reviewService, IUserContextService userContextService)
        {
            this._reviewService = reviewService;
            this._userContextService = userContextService;
        }

        [Authorize]
        public async Task<IActionResult> Create(int id)
        {
            ReviewViewModel model = await this._reviewService.GetCreateModelAsync(id);
            return this.View(model);
        }

        // POST: Review/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create(ReviewViewModel reviewViewModel)
        {
            if (this.ModelState.IsValid)
            {
                int userId = this._userContextService.UserId ?? throw new Exception("User not authenticated");
                _ = await this._reviewService.CreateReviewAsync(reviewViewModel, userId);
                return this.RedirectToAction("Details", "Map", new { id = reviewViewModel.LocationId });
            }
            return this.View(reviewViewModel);
        }

        // GET: Review/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            int userId = this._userContextService.UserId ?? throw new Exception("User not authenticated");
            ReviewViewModel model = await this._reviewService.GetEditModelAsync(id, userId);
            return this.View(model);
        }

        // POST: Review/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, ReviewViewModel reviewViewModel)
        {
            if (this.ModelState.IsValid)
            {
                int userId = this._userContextService.UserId ?? throw new Exception("User not authenticated");
                await this._reviewService.EditReviewAsync(id, reviewViewModel, userId);
                return this.RedirectToAction("Details", "Map", new { id = reviewViewModel.LocationId });
            }
            return this.View(reviewViewModel);
        }

        // POST: Review/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            int userId = this._userContextService.UserId ?? throw new Exception("User not authenticated");
            bool isAdmin = this.User.IsInRole("Admin");
            int locationId = await this._reviewService.DeleteReviewAsync(id, userId, isAdmin);
            return this.RedirectToAction("Details", "Map", new { id = locationId });
        }
    }
}