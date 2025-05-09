using MapHive.Services;
using Microsoft.AspNetCore.Mvc;

namespace MapHive.Models.Exceptions.UserFriendlyExceptions
{
    public class UserFriendlyExceptionMessageViewComponent : ViewComponent
    {
        private readonly IUserFriendlyExceptionService _userFriendlyExceptionService;

        public UserFriendlyExceptionMessageViewComponent(IUserFriendlyExceptionService userFriendlyExceptionService)
        {
            this._userFriendlyExceptionService = userFriendlyExceptionService;
        }

        public IViewComponentResult Invoke()
        {
            if (!string.IsNullOrEmpty(this._userFriendlyExceptionService.Message))
            {
                // Get the message type, default to Blue if not specified
                string messageTypeString = this._userFriendlyExceptionService.Type ?? "Blue";

                // Try to parse the message type from the string
                if (!Enum.TryParse(messageTypeString, out UserFriendlyExceptionBase.MessageType messageType))
                {
                    messageType = UserFriendlyExceptionBase.MessageType.Blue;
                }

                // Clear the messages from session after retrieving them
                this._userFriendlyExceptionService.Clear();

                // Create a ViewModel to pass both the message and type to the view
                UserFriendlyExceptionMessageViewModel viewModel = new()
                {
                    Message = this._userFriendlyExceptionService.Message,
                    MessageType = messageType
                };

                return this.View(viewModel);
            }

            // Return empty view if no message
            return this.View(new UserFriendlyExceptionMessageViewModel());
        }
    }

    public class UserFriendlyExceptionMessageViewModel
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