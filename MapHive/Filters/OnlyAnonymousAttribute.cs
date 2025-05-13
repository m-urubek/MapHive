namespace MapHive.Filters
{
    using System;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Filters;

    /// <summary>
    /// Allows only unauthenticated (anonymous) users to access an action or controller.
    /// If the user is already authenticated, they will be redirected to a specified action.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class OnlyAnonymousAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// The action to redirect authenticated users to. Defaults to "Profile".
        /// </summary>
        public string RedirectAction { get; set; } = "Profile";

        /// <summary>
        /// The controller to redirect authenticated users to. Defaults to "Account".
        /// </summary>
        public string RedirectController { get; set; } = "Account";

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            System.Security.Claims.ClaimsPrincipal user = context.HttpContext.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                // Redirect authenticated users away from anonymous-only endpoints
                context.Result = new RedirectToActionResult(actionName: RedirectAction, controllerName: RedirectController, routeValues: null);
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}