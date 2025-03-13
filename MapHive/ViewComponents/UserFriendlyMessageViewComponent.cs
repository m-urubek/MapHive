using Microsoft.AspNetCore.Mvc;

namespace MapHive.ViewComponents
{
    public class UserFriendlyMessageViewComponent : ViewComponent
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserFriendlyMessageViewComponent(IHttpContextAccessor httpContextAccessor)
        {
            this._httpContextAccessor = httpContextAccessor;
        }

        public IViewComponentResult Invoke()
        {
            string message = this._httpContextAccessor.HttpContext.Session.GetString("UserFriendlyMessage");

            if (!string.IsNullOrEmpty(message))
            {
                // Clear the message from session after retrieving it
                this._httpContextAccessor.HttpContext.Session.Remove("UserFriendlyMessage");
                return this.View(model: message);
            }

            // Return empty view if no message
            return this.View(model: string.Empty);
        }
    }
}