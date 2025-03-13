using MapHive.Models;
using MapHive.Services;

namespace MapHive.Middleware
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public ErrorHandlingMiddleware(RequestDelegate next)
        {
            this._next = next;
        }

        public async Task InvokeAsync(HttpContext context, LogManager logManager)
        {
            try
            {
                await this._next(context);
            }
            catch (UserFriendlyException ex)
            {
                // For user-friendly exceptions, don't log them as errors but show to the user
                await this.HandleUserFriendlyExceptionAsync(context, ex);

                // Don't re-throw the exception since we've handled it by showing a friendly message
            }
            catch (Exception ex)
            {
                this.HandleExceptionAsync(context, ex, logManager);
                throw; // Re-throw the exception to let the built-in exception handlers deal with it
            }
        }

        private void HandleExceptionAsync(HttpContext context, Exception exception, LogManager logManager)
        {
            // Log the exception with the LogManager
            logManager.Error(
                message: exception.Message,
                source: "ErrorHandlingMiddleware",
                exception: exception,
                additionalData: $"{{\"path\": \"{context.Request.Path}\", \"method\": \"{context.Request.Method}\"}}"
            );
        }

        private async Task HandleUserFriendlyExceptionAsync(HttpContext context, UserFriendlyException exception)
        {
            // Store the exception message in TempData so it can be displayed on the next page
            context.Session.SetString("UserFriendlyMessage", exception.Message);

            // Check if the request is an AJAX request
            bool isAjaxRequest = context.Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (isAjaxRequest)
            {
                // For AJAX requests, return a JSON response with the message
                context.Response.StatusCode = 200; // Use 200 instead of error code since this is user-friendly
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync($"{{\"userFriendlyMessage\": \"{exception.Message}\"}}");
            }
            else
            {
                // For regular requests, redirect back to the same page (or referrer if available)
                string redirectUrl = context.Request.Headers["Referer"].ToString();
                if (string.IsNullOrEmpty(redirectUrl))
                {
                    redirectUrl = context.Request.Path;
                }

                context.Response.Redirect(redirectUrl);
            }
        }
    }
}