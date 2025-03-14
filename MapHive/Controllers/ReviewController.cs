using MapHive.Models;
using MapHive.Models.ViewModels;
using MapHive.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MapHive.Controllers
{
    public class ReviewController : Controller
    {
        private readonly IReviewRepository _reviewRepository;
        private readonly IMapLocationRepository _locationRepository;
        private readonly IDiscussionRepository _discussionRepository;

        public ReviewController(
            IReviewRepository reviewRepository,
            IMapLocationRepository locationRepository,
            IDiscussionRepository discussionRepository)
        {
            this._reviewRepository = reviewRepository;
            this._locationRepository = locationRepository;
            this._discussionRepository = discussionRepository;
        }

        // GET: Review/Create/5 (5 is the location ID)
        [Authorize]
        public async Task<IActionResult> Create(int id)
        {
            // Check if location exists
            MapLocation? location = await this._locationRepository.GetLocationByIdAsync(id);
            if (location == null)
            {
                return this.NotFound();
            }

            // Check if user has already reviewed this location
            int userId = this.GetCurrentUserId();
            bool hasReviewed = await this._reviewRepository.HasUserReviewedLocationAsync(userId, id);
            if (hasReviewed)
            {
                this.TempData["Error"] = "You have already reviewed this location.";
                return this.RedirectToAction("Details", "Map", new { id });
            }

            ReviewViewModel model = new()
            {
                LocationId = id,
                LocationName = location.Name,
                ReviewText = string.Empty
            };

            return this.View(model);
        }

        // POST: Review/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create(ReviewViewModel model)
        {
            if (this.ModelState.IsValid)
            {
                // Check if location exists
                MapLocation? location = await this._locationRepository.GetLocationByIdAsync(model.LocationId);
                if (location == null)
                {
                    return this.NotFound();
                }

                // Check if user has already reviewed this location
                int userId = this.GetCurrentUserId();
                bool hasReviewed = await this._reviewRepository.HasUserReviewedLocationAsync(userId, model.LocationId);
                if (hasReviewed)
                {
                    this.TempData["Error"] = "You have already reviewed this location.";
                    return this.RedirectToAction("Details", "Map", new { id = model.LocationId });
                }

                // Create the review
                Review review = new()
                {
                    LocationId = model.LocationId,
                    UserId = userId,
                    Rating = model.Rating,
                    ReviewText = model.ReviewText,
                    IsAnonymous = model.IsAnonymous,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                review = await this._reviewRepository.AddReviewAsync(review);

                // Create a review thread
                _ = await this._discussionRepository.CreateReviewThreadAsync(review.Id, model.LocationId, userId);

                return this.RedirectToAction("Details", "Map", new { id = model.LocationId });
            }

            // If we got this far, something failed, redisplay form
            return this.View(model);
        }

        // GET: Review/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            Review? review = await this._reviewRepository.GetReviewByIdAsync(id);
            if (review == null)
            {
                return this.NotFound();
            }

            // Only allow the review author to edit
            int userId = this.GetCurrentUserId();
            if (review.UserId != userId)
            {
                return this.Forbid();
            }

            // Get the location
            MapLocation? location = await this._locationRepository.GetLocationByIdAsync(review.LocationId);
            if (location == null)
            {
                return this.NotFound();
            }

            ReviewViewModel model = new()
            {
                LocationId = review.LocationId,
                Rating = review.Rating,
                ReviewText = review.ReviewText,
                IsAnonymous = review.IsAnonymous,
                LocationName = location.Name
            };

            return this.View(model);
        }

        // POST: Review/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, ReviewViewModel model)
        {
            Review? review = await this._reviewRepository.GetReviewByIdAsync(id);
            if (review == null)
            {
                return this.NotFound();
            }

            // Only allow the review author to edit
            int userId = this.GetCurrentUserId();
            if (review.UserId != userId)
            {
                return this.Forbid();
            }

            if (this.ModelState.IsValid)
            {
                review.Rating = model.Rating;
                review.ReviewText = model.ReviewText;
                review.IsAnonymous = model.IsAnonymous;
                review.UpdatedAt = DateTime.UtcNow;

                _ = await this._reviewRepository.UpdateReviewAsync(review);
                return this.RedirectToAction("Details", "Map", new { id = review.LocationId });
            }

            // If we got this far, something failed, redisplay form
            model.LocationName = (await this._locationRepository.GetLocationByIdAsync(review.LocationId))?.Name ?? "";
            return this.View(model);
        }

        // POST: Review/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            Review? review = await this._reviewRepository.GetReviewByIdAsync(id);
            if (review == null)
            {
                return this.NotFound();
            }

            // Only allow the review author or admin to delete
            int userId = this.GetCurrentUserId();
            bool isAdmin = this.User.IsInRole("Admin");
            if (!isAdmin && review.UserId != userId)
            {
                return this.Forbid();
            }

            // Check if this review has a review thread with messages
            string reviewText = review.ReviewText;
            _ = review.IsAnonymous;
            _ = review.UserId;
            int locationId = review.LocationId;

            // Get all threads related to this review
            DiscussionThread? allThreads = await this._discussionRepository.GetThreadByIdAsync(review.Id);
            DiscussionThread? reviewThread = null;

            if (allThreads != null && allThreads.IsReviewThread && allThreads.ReviewId == review.Id)
            {
                reviewThread = allThreads;
            }

            // Delete the review
            _ = await this._reviewRepository.DeleteReviewAsync(id);

            // If review thread exists and has messages, convert it to discussion thread
            if (reviewThread != null && reviewThread.Messages.Count > 0)
            {
                _ = await this._discussionRepository.ConvertReviewThreadToDiscussionAsync(
                    reviewThread.Id,
                    reviewText);
            }

            return this.RedirectToAction("Details", "Map", new { id = locationId });
        }

        private int GetCurrentUserId()
        {
            string? userId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int id)
                ? throw new Exception("User ID not found or is invalid")
                : id;
        }
    }
}