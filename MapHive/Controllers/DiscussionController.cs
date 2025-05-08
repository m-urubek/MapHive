using MapHive.Models.ViewModels;
using MapHive.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MapHive.Controllers
{
    public class DiscussionController : Controller
    {
        private readonly IDiscussionService _discussionService;
        private readonly IUserContextService _userContextService;

        public DiscussionController(IDiscussionService discussionService, IUserContextService userContextService)
        {
            this._discussionService = discussionService;
            this._userContextService = userContextService;
        }

        public async Task<IActionResult> Thread(int id)
        {
            // Retrieve combined thread details and new message model
            ThreadPageViewModel pageModel = await this._discussionService.GetThreadPageViewModelAsync(id);
            // Pass the new message form model to ViewBag
            this.ViewBag.MessageViewModel = pageModel.NewMessage;
            // Render thread details
            return this.View(pageModel.ThreadDetails);
        }

        // GET: Discussion/Create/5 (5 is the location ID)
        [Authorize]
        public async Task<IActionResult> Create(int id)
        {
            DiscussionThreadViewModel model = await this._discussionService.GetCreateModelAsync(id);
            return this.View(model);
        }

        // POST: Discussion/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create(DiscussionThreadViewModel discussionThreadViewModel)
        {
            if (this.ModelState.IsValid)
            {
                int userId = this._userContextService.UserId ?? throw new Exception("User not authenticated");
                Models.RepositoryModels.DiscussionThreadGet created = await this._discussionService.CreateDiscussionThreadAsync(discussionThreadViewModel, userId);
                return this.RedirectToAction("Thread", new { id = created.Id });
            }

            // If we got this far, something failed, redisplay form
            return this.View(discussionThreadViewModel);
        }

        // POST: Discussion/AddMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> AddMessage(ThreadMessageViewModel threadMessageViewModel)
        {
            if (this.ModelState.IsValid)
            {
                int userId = this._userContextService.UserId ?? throw new Exception("User not authenticated");
                _ = await this._discussionService.AddMessageAsync(threadMessageViewModel, userId);
                return this.RedirectToAction("Thread", new { id = threadMessageViewModel.ThreadId });
            }

            // If we got this far, something failed, redirect back to the thread
            return this.RedirectToAction("Thread", new { id = threadMessageViewModel.ThreadId });
        }

        // POST: Discussion/DeleteMessage/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            int userId = this._userContextService.UserId ?? throw new Exception("User not authenticated");
            bool isAdmin = this.User.IsInRole("Admin");
            await this._discussionService.DeleteMessageAsync(id, userId, isAdmin);
            // After deletion, threadId is not directly available; service ensures message thread exists before deletion
            // Redirect back to thread (client may need to know threadId separately)
            return this.RedirectToAction("Thread", new { id });
        }

        // POST: Discussion/DeleteThread/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteThread(int id)
        {
            int locationId = await this._discussionService.DeleteThreadAsync(id);
            return this.RedirectToAction("Details", "Map", new { id = locationId });
        }
    }
}