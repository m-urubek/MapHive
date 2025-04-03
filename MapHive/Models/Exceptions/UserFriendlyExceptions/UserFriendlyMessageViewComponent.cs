using Microsoft.AspNetCore.Mvc;

namespace MapHive.Models.Exceptions.UserFriendlyExceptions
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
            HttpContext? httpContext = this._httpContextAccessor.HttpContext;
            string message = httpContext.Session.GetString("UserFriendlyMessage");

            if (!string.IsNullOrEmpty(message))
            {
                // Get the message type, default to Blue if not specified
                string messageTypeString = httpContext.Session.GetString("UserFriendlyMessageType") ?? "Blue";

                // Try to parse the message type from the string
                if (!Enum.TryParse(messageTypeString, out UserFriendlyExceptionBase.MessageType messageType))
                {
                    messageType = UserFriendlyExceptionBase.MessageType.Blue;
                }

                // Clear the messages from session after retrieving them
                httpContext.Session.Remove("UserFriendlyMessage");
                httpContext.Session.Remove("UserFriendlyMessageType");

                // Create a ViewModel to pass both the message and type to the view
                UserFriendlyMessageViewModel viewModel = new()
                {
                    Message = message,
                    MessageType = messageType
                };

                return this.View(viewModel);
            }

            // Return empty view if no message
            return this.View(new UserFriendlyMessageViewModel());
        }
    }

    public class UserFriendlyMessageViewModel
    {
        public string Message { get; set; } = string.Empty;
        public UserFriendlyExceptionBase.MessageType MessageType { get; set; } = UserFriendlyExceptionBase.MessageType.Blue;

        // Helper property to get the Bootstrap alert class based on message type
        public string AlertClass => this.MessageType switch
        {
            UserFriendlyExceptionBase.MessageType.Blue => "alert-info",
            UserFriendlyExceptionBase.MessageType.Orange => "alert-warning",
            UserFriendlyExceptionBase.MessageType.Red => "alert-danger",
            _ => "alert-info"
        };
    }
}