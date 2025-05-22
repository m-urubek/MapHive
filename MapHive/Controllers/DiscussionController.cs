namespace MapHive.Controllers;

using MapHive.Models.PageModels;
using MapHive.Repositories;
using MapHive.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

public class DiscussionController(
    IDiscussionService _discussionService,
    IUserContextService _userContextService,
    IDiscussionRepository _discussionRepository
) : Controller
{

    [HttpGet("Discussion/Thread/{id:int:required}")]
    public async Task<IActionResult> Thread(int id)
    {
        ThreadDisplayPageModel pageModel = await _discussionService.GetThreadDisplayPageModelAsync(threadId: id);
        return View(model: pageModel);
    }

    [HttpGet("Discussion/Create/{id:int:required}")]
    [Authorize]
    public async Task<IActionResult> Create(int id)
    {
        ThreadCreatePageModel model = await _discussionService.GetThreadCreatePageModelAsync(locationId: id);
        return View(model: model);
    }

    // POST: Discussion/Create
    [HttpPost("Discussion/Create/{id:int:required}")]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Create(int id, ThreadCreatePageModel discussionThreadPageModel)
    {
        if (!ModelState.IsValid)
            return View(model: discussionThreadPageModel);

        int createdThreadId = await _discussionService.CreateDiscussionThreadAsync(
            locationId: id,
            threadName: discussionThreadPageModel.ThreadName!,
            reviewId: null,
            isAnonymous: false,
            initialMessage: discussionThreadPageModel.InitialMessage!
            );
        return RedirectToAction(actionName: "Thread", routeValues: new { id = createdThreadId });
    }

    // POST: /AddMessage
    [HttpPost("Discussion/AddMessage/{id:int:required}")]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> AddMessage(int id, ThreadMessagePageModel threadMessagePageModel)
    {
        if (!ModelState.IsValid)
        {
            return PartialView(viewName: "_ReplyFormPartial", model: threadMessagePageModel);
        }

        _ = await _discussionRepository.CreateMessageAsync(
            threadId: id,
            authorId: _userContextService.AccountIdOrThrow,
            messageText: threadMessagePageModel.MessageText!,
            isInitialMessage: false
        );
        return RedirectToAction(actionName: "Thread", routeValues: new { id });
    }

    // POST: Discussion/DeleteMessage/5
    [HttpPost("Discussion/DeleteMessage/{id:int:required}")]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> DeleteMessage(int id)
    {
        await _discussionService.DeleteMessageAsync(messageId: id);
        return this.ReddirectToReferer();
    }

    [HttpPost("Discussion/Delete/{id:int:required}/{locationId:int:required}")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteThread(int id, int locationId)
    {
        await _discussionRepository.DeleteThreadOrThrowAsync(id);
        return RedirectToAction(actionName: "Details", controllerName: "Map", routeValues: new { id = locationId });
    }
}
