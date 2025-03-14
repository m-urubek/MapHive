using MapHive.Models;
using MapHive.Models.ViewModels;
using MapHive.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MapHive.Controllers
{
    public class DiscussionController : Controller
    {
        private readonly IDiscussionRepository _discussionRepository;
        private readonly IMapLocationRepository _locationRepository;
        private readonly IReviewRepository _reviewRepository;

        public DiscussionController(
            IDiscussionRepository discussionRepository,
            IMapLocationRepository locationRepository,
            IReviewRepository reviewRepository)
        {
            this._discussionRepository = discussionRepository;
            this._locationRepository = locationRepository;
            this._reviewRepository = reviewRepository;
        }

        // GET: Discussion/Thread/5
        public async Task<IActionResult> Thread(int id)
        {
            DiscussionThread? thread = await this._discussionRepository.GetThreadByIdAsync(id);
            if (thread == null)
            {
                return this.NotFound();
            }

            // If it's a review thread, get the review
            if (thread.IsReviewThread && thread.ReviewId.HasValue)
            {
                thread.Review = await this._reviewRepository.GetReviewByIdAsync(thread.ReviewId.Value);
            }

            // Get the location
            thread.Location = await this._locationRepository.GetLocationByIdAsync(thread.LocationId);

            // Create a view model for adding a new message
            ThreadMessageViewModel messageViewModel = new()
            {
                ThreadId = thread.Id,
                ThreadName = thread.ThreadName,
                MessageText = string.Empty
            };

            this.ViewBag.MessageViewModel = messageViewModel;

            return this.View(thread);
        }

        // GET: Discussion/Create/5 (5 is the location ID)
        [Authorize]
        public async Task<IActionResult> Create(int id)
        {
            // Check if location exists
            MapLocation? location = await this._locationRepository.GetLocationByIdAsync(id);
            if (location == null)
            {
                return this.NotFound();
            }

            DiscussionThreadViewModel model = new()
            {
                LocationId = id,
                LocationName = location.Name,
                ThreadName = string.Empty,
                InitialMessage = string.Empty
            };

            return this.View(model);
        }

        // POST: Discussion/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create(DiscussionThreadViewModel model)
        {
            if (this.ModelState.IsValid)
            {
                // Check if location exists
                MapLocation? location = await this._locationRepository.GetLocationByIdAsync(model.LocationId);
                if (location == null)
                {
                    return this.NotFound();
                }

                // Create the thread
                DiscussionThread thread = new()
                {
                    LocationId = model.LocationId,
                    UserId = this.GetCurrentUserId(),
                    ThreadName = model.ThreadName,
                    IsReviewThread = false,
                    CreatedAt = DateTime.UtcNow
                };

                thread = await this._discussionRepository.CreateDiscussionThreadAsync(thread, model.InitialMessage);

                return this.RedirectToAction("Thread", new { id = thread.Id });
            }

            // If we got this far, something failed, redisplay form
            return this.View(model);
        }

        // POST: Discussion/AddMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> AddMessage(ThreadMessageViewModel model)
        {
            if (this.ModelState.IsValid)
            {
                // Check if thread exists
                DiscussionThread? thread = await this._discussionRepository.GetThreadByIdAsync(model.ThreadId);
                if (thread == null)
                {
                    return this.NotFound();
                }

                // Create the message
                ThreadMessage message = new()
                {
                    ThreadId = model.ThreadId,
                    UserId = this.GetCurrentUserId(),
                    MessageText = model.MessageText,
                    IsInitialMessage = false,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow
                };

                _ = await this._discussionRepository.AddMessageAsync(message);

                return this.RedirectToAction("Thread", new { id = model.ThreadId });
            }

            // If we got this far, something failed, redirect back to the thread
            return this.RedirectToAction("Thread", new { id = model.ThreadId });
        }

        // POST: Discussion/DeleteMessage/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            ThreadMessage? message = await this._discussionRepository.GetMessageByIdAsync(id);
            if (message == null)
            {
                return this.NotFound();
            }

            // Only allow the message author or admin to delete
            int userId = this.GetCurrentUserId();
            bool isAdmin = this.User.IsInRole("Admin");
            if (!isAdmin && message.UserId != userId)
            {
                return this.Forbid();
            }

            _ = await this._discussionRepository.DeleteMessageAsync(id, userId);

            return this.RedirectToAction("Thread", new { id = message.ThreadId });
        }

        // POST: Discussion/DeleteThread/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteThread(int id)
        {
            DiscussionThread? thread = await this._discussionRepository.GetThreadByIdAsync(id);
            if (thread == null)
            {
                return this.NotFound();
            }

            int locationId = thread.LocationId;
            _ = await this._discussionRepository.DeleteThreadAsync(id);

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