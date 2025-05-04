using AutoMapper;
using MapHive.Models.RepositoryModels;
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
        private readonly IMapLocationRepository _mapLocationRepository;
        private readonly IDiscussionRepository _discussionRepository;
        private readonly IMapper _mapper;
        public ReviewController(IDiscussionRepository discussionRepository, IMapLocationRepository mapLocationRepository, IReviewRepository reviewRepository, IMapper mapper)
        {
            this._discussionRepository = discussionRepository;
            this._mapLocationRepository = mapLocationRepository;
            this._reviewRepository = reviewRepository;
            this._mapper = mapper;
        }

        [Authorize]
        public async Task<IActionResult> Create(int id)
        {
            // Check if location exists
            MapLocationGet? location = await this._mapLocationRepository.GetLocationByIdAsync(id);
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
                MapLocationGet? location = await this._mapLocationRepository.GetLocationByIdAsync(model.LocationId);
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

                // Create the review DTO
                ReviewCreate reviewDto = this._mapper.Map<ReviewCreate>(model);
                reviewDto.UserId = userId;
                ReviewGet createdReview = await this._reviewRepository.AddReviewAsync(reviewDto);

                // Create a review thread using the new DTO
                ReviewThreadCreate reviewThreadDto = new()
                {
                    ReviewId = createdReview.Id,
                    LocationId = model.LocationId,
                    UserId = this.GetCurrentUserId(),
                    Username = this.User.Identity?.Name ?? string.Empty,
                    ReviewTitle = model.LocationName
                };
                _ = await this._discussionRepository.CreateReviewThreadAsync(reviewThreadDto);

                return this.RedirectToAction("Details", "Map", new { id = model.LocationId });
            }

            // If we got this far, something failed, redisplay form
            return this.View(model);
        }

        // GET: Review/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            ReviewGet? review = await this._reviewRepository.GetReviewByIdAsync(id);
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
            MapLocationGet? location = await this._mapLocationRepository.GetLocationByIdAsync(review.LocationId);
            if (location == null)
            {
                return this.NotFound();
            }

            ReviewViewModel model = this._mapper.Map<ReviewViewModel>(review);
            model.LocationName = (await this._mapLocationRepository.GetLocationByIdAsync(review.LocationId))?.Name ?? string.Empty;

            return this.View(model);
        }

        // POST: Review/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, ReviewViewModel model)
        {
            ReviewGet? review = await this._reviewRepository.GetReviewByIdAsync(id);
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
                ReviewUpdate updateDto = this._mapper.Map<ReviewUpdate>(model);
                updateDto.Id = id;
                updateDto.UserId = userId;
                updateDto.LocationId = review.LocationId;
                _ = await this._reviewRepository.UpdateReviewAsync(updateDto);
                return this.RedirectToAction("Details", "Map", new { id = review.LocationId });
            }

            // If we got this far, something failed, redisplay form
            model.LocationName = (await this._mapLocationRepository.GetLocationByIdAsync(review.LocationId))?.Name ?? string.Empty;
            return this.View(model);
        }

        // POST: Review/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            ReviewGet? review = await this._reviewRepository.GetReviewByIdAsync(id);
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

            // Delete the review
            _ = await this._reviewRepository.DeleteReviewAsync(id);
            // If a review thread was created and has messages, convert it to a discussion thread
            DiscussionThreadGet? threadDto = await this._discussionRepository.GetThreadByIdAsync(review.Id);
            if (threadDto != null && threadDto.IsReviewThread && threadDto.ReviewId == review.Id && threadDto.Messages.Any())
            {
                _ = await this._discussionRepository.ConvertReviewThreadToDiscussionAsync(
                    threadDto.Id,
                    reviewText);
            }

            return this.RedirectToAction("Details", "Map", new { id = locationId });
        }

        private int GetCurrentUserId()
        {
            string? userId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int id)
                ? throw new Exception("UserLogin ID not found or is invalid")
                : id;
        }
    }
}