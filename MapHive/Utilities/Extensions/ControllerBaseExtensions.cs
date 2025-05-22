namespace MapHive;

using Microsoft.AspNetCore.Mvc;

public static class ControllerBaseExtensions
{
    public static ActionResult ReddirectToReferer(this ControllerBase controller)
    {
        string? referer = controller.Request?.Headers?.Referer.ToString();
        return !string.IsNullOrWhiteSpace(referer) ? controller.Redirect(referer) : controller.RedirectToAction(actionName: "Index", controllerName: "Home");
    }
}
