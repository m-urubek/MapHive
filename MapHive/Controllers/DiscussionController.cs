namespace MapHive.Controllers
{
    using MapHive.Models.ViewModels;
    using MapHive.Services;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    public class DiscussionController(IDiscussionService discussionService, IUserContextService userContextService) : Controller
    {
        private readonly IDiscussionService _discussionService = discussionService;
        private readonly IUserContextService _userContextService = userContextService;

        public async Task<IActionResult> Thread(int id)
        {
            // Retrieve combined thread details and new message model
            ThreadPageViewModel pageModel = await _discussionService.GetThreadPageViewModelAsync(threadId: id);
            // Pass the new message form model to ViewBag
            ViewBag.MessageViewModel = pageModel.NewMessage;
            // Render thread details
            return View(model: pageModel.ThreadDetails);
        }

        // GET: Discussion/Create/5 (5 is the location ID)
        [Authorize]
        public async Task<IActionResult> Create(int id)
        {
            DiscussionThreadViewModel model = await _discussionService.GetCreateModelAsync(locationId: id);
            return View(model: model);
        }

        // POST: Discussion/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create(DiscussionThreadViewModel discussionThreadViewModel)
        {
            if (ModelState.IsValid)
            {
                int accountId = _userContextService.AccountIdRequired;
                Models.RepositoryModels.DiscussionThreadGet created = await _discussionService.CreateDiscussionThreadAsync(model: discussionThreadViewModel, accountId: accountId);
                return RedirectToAction(actionName: "Thread", routeValues: new { id = created.Id });
            }

            // If we got this far, something failed, redisplay form
            return View(model: discussionThreadViewModel);
        }

        // POST: Discussion/AddMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> AddMessage(ThreadMessageViewModel threadMessageViewModel)
        {
            if (ModelState.IsValid)
            {
                int accountId = _userContextService.AccountIdRequired;
                _ = await _discussionService.AddMessageAsync(model: threadMessageViewModel, accountId: accountId);
                return RedirectToAction(actionName: "Thread", routeValues: new { id = threadMessageViewModel.ThreadId });
            }

            // If we got this far, something failed, redirect back to the thread
            return RedirectToAction(actionName: "Thread", routeValues: new { id = threadMessageViewModel.ThreadId });
        }

        // POST: Discussion/DeleteMessage/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            int accountId = _userContextService.AccountIdRequired;
            await _discussionService.DeleteMessageAsync(messageId: id, accountId: accountId);
            // After deletion, threadId is not directly available; service ensures message thread exists before deletion
            // Redirect back to thread (client may need to know threadId separately)
            return RedirectToAction(actionName: "Thread", routeValues: new { id });
        }

        // POST: Discussion/DeleteThread/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteThread(int id)
        {
            int locationId = await _discussionService.DeleteThreadAsync(threadId: id);
            return RedirectToAction(actionName: "Details", controllerName: "Map", routeValues: new { id = locationId });
        }
    }
}