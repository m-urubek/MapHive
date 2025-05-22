namespace MapHive.Models.Exceptions.UserFriendlyExceptions;

using MapHive.Services;
using Microsoft.AspNetCore.Mvc;

public class UserFriendlyExceptionMessageViewComponent(IUserFriendlyExceptionService userFriendlyExceptionService) : ViewComponent
{
    private readonly IUserFriendlyExceptionService _userFriendlyExceptionService = userFriendlyExceptionService;

    public IViewComponentResult Invoke()
    {
        string? message = _userFriendlyExceptionService.Message;
        if (!string.IsNullOrEmpty(message))
        {
            // Get the message type, default to Blue if not specified
            string messageTypeString = _userFriendlyExceptionService.Type ?? "Blue";

            // Try to parse the message type from the string
            if (!Enum.TryParse(messageTypeString, out UserFriendlyExceptionBase.MessageType messageType))
            {
                messageType = UserFriendlyExceptionBase.MessageType.Blue;
            }

            // Clear the messages from session after retrieving them
            _userFriendlyExceptionService.Clear();

            // Create a PageModel to pass both the message and type to the view
            UserFriendlyExceptionMessagePageModel pageModel = new()
            {
                Message = message,
                MessageType = messageType
            };

            return View(model: pageModel);
        }

        // Return empty view if no message
        return View(model: new UserFriendlyExceptionMessagePageModel());
    }
}

public class UserFriendlyExceptionMessagePageModel
{
    public string Message { get; set; } = string.Empty;
    public UserFriendlyExceptionBase.MessageType MessageType { get; set; } = UserFriendlyExceptionBase.MessageType.Blue;

    // Helper property to get the Bootstrap alert class based on message type
    public string AlertClass => MessageType switch
    {
        UserFriendlyExceptionBase.MessageType.Blue => "alert-info",
        UserFriendlyExceptionBase.MessageType.Orange => "alert-warning",
        UserFriendlyExceptionBase.MessageType.Red => "alert-danger",
        _ => "alert-info"
    };
}