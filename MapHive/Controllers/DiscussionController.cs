using AutoMapper;
using MapHive.Models.RepositoryModels;
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
        private readonly IMapLocationRepository _mapLocationRepository;
        private readonly IReviewRepository _reviewRepository;
        private readonly IMapper _mapper;

        public DiscussionController(IDiscussionRepository discussionRepository, IMapLocationRepository mapLocationRepository, IReviewRepository reviewRepository, IMapper mapper)
        {
            this._discussionRepository = discussionRepository;
            this._mapLocationRepository = mapLocationRepository;
            this._reviewRepository = reviewRepository;
            this._mapper = mapper;
        }

        public async Task<IActionResult> Thread(int id)
        {
            DiscussionThreadGet? dto = await this._discussionRepository.GetThreadByIdAsync(id);
            if (dto == null)
            {
                return this.NotFound();
            }

            // Map repository DTO to view model using AutoMapper
            ThreadDetailsViewModel viewModel = this._mapper.Map<ThreadDetailsViewModel>(dto);
            viewModel.Messages = (await this._discussionRepository.GetMessagesByThreadIdAsync(dto.Id)).ToList();
            viewModel.Location = await this._mapLocationRepository.GetLocationByIdAsync(dto.LocationId)
                ?? throw new Exception("Location not found");

            if (dto.IsReviewThread && dto.ReviewId.HasValue)
            {
                viewModel.Review = await this._reviewRepository.GetReviewByIdAsync(dto.ReviewId.Value);
            }

            // Prepare view model for adding a new message
            ThreadMessageViewModel messageViewModel = new()
            {
                ThreadId = dto.Id,
                ThreadName = dto.ThreadName,
                MessageText = string.Empty
            };

            this.ViewBag.MessageViewModel = messageViewModel;

            return this.View(viewModel);
        }

        // GET: Discussion/Create/5 (5 is the location ID)
        [Authorize]
        public async Task<IActionResult> Create(int id)
        {
            // Check if location exists
            MapLocationGet? location = await this._mapLocationRepository.GetLocationByIdAsync(id);
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
                MapLocationGet? location = await this._mapLocationRepository.GetLocationByIdAsync(model.LocationId);
                if (location == null)
                {
                    return this.NotFound();
                }

                // Map view model to create DTO
                DiscussionThreadCreate dtoForCreate = this._mapper.Map<DiscussionThreadCreate>(model);
                dtoForCreate.UserId = this.GetCurrentUserId();
                dtoForCreate.IsReviewThread = false;
                dtoForCreate.ReviewId = null;

                DiscussionThreadGet created = await this._discussionRepository.CreateDiscussionThreadAsync(dtoForCreate, model.InitialMessage);

                return this.RedirectToAction("Thread", new { id = created.Id });
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
                DiscussionThreadGet? threadDto = await this._discussionRepository.GetThreadByIdAsync(model.ThreadId);
                if (threadDto == null)
                {
                    return this.NotFound();
                }

                // Map view model to message create DTO
                ThreadMessageCreate messageDto = this._mapper.Map<ThreadMessageCreate>(model);
                messageDto.UserId = this.GetCurrentUserId();
                messageDto.IsInitialMessage = false;

                _ = await this._discussionRepository.AddMessageAsync(messageDto);

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
            ThreadMessageGet? message = await this._discussionRepository.GetMessageByIdAsync(id);
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
            DiscussionThreadGet? thread = await this._discussionRepository.GetThreadByIdAsync(id);
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
                ? throw new Exception("UserLogin ID not found or is invalid")
                : id;
        }
    }
}