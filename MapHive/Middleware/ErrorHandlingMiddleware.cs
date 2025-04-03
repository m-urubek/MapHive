using MapHive.Models.Exceptions;
using MapHive.Services;
using MapHive.Singletons;

namespace MapHive.Middleware
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public ErrorHandlingMiddleware(RequestDelegate next)
        {
            this._next = next;
        }

        public async Task InvokeAsync(HttpContext context, LogManagerService logManager)
        {
            try
            {
                await this._next(context);
            }
            catch (UserFriendlyExceptionBase ex)
            {
                // For user-friendly exceptions, don't log them as errors but show to the user
                await this.HandleUserFriendlyExceptionAsync(context, ex);

                // Don't re-throw the exception since we've handled it by showing a friendly message
            }
            catch (WarningException ex)
            {
                await this.HandleUserFriendlyExceptionAsync(context, new OrangeUserException(ex.Message));
                logManager.Warning(ex.Message,
                    "ErrorHandlingMiddleware",
                    ex,
                    $"{{\"path\": \"{context.Request.Path}\", \"method\": \"{context.Request.Method}\"}}"
                );
            }
            catch (NonCriticalException ex)
            {
                try
                {
                    if (MainClient.AppSettings.DevelopmentMode)
                    {
                        //TODO after admin panel is done, display link to some page in admin panel displaying details of the exception
                        await this.HandleUserFriendlyExceptionAsync(context, new OrangeUserException(ex.ToString()));
                    }
                    else
                    {
                        // Show a generic error message to the user
                        await this.HandleUserFriendlyExceptionAsync(context, new OrangeUserException("Internal Server Error!"));
                    }
                }
                catch { } // Don't let UI message display issues prevent error logging

                logManager.Error(
                    message: ex.Message,
                    source: "ErrorHandlingMiddleware",
                    exception: ex,
                    additionalData: $"{{\"path\": \"{context.Request.Path}\", \"method\": \"{context.Request.Method}\"}}"
                );
            }
            catch (Exception ex)
            {
                try
                {
                    if (MainClient.AppSettings.DevelopmentMode)
                    {
                        //TODO after admin panel is done, display link to some page in admin panel displaying details of the exception
                        await this.HandleUserFriendlyExceptionAsync(context, new RedUserException(ex.ToString()));
                    }
                    else
                    {
                        // Show a generic error message to the user
                        await this.HandleUserFriendlyExceptionAsync(context, new RedUserException("Internal Server Error!"));
                    }
                }
                catch { } // Don't let UI message display issues prevent error logging

                logManager.Critical(
                    message: ex.Message,
                    source: "ErrorHandlingMiddleware",
                    exception: ex,
                    additionalData: $"{{\"path\": \"{context.Request.Path}\", \"method\": \"{context.Request.Method}\"}}"
                );
            }
        }

        private async Task HandleUserFriendlyExceptionAsync(HttpContext context, UserFriendlyExceptionBase exception)
        {
            // Convert exception type to string for storage in session
            string messageTypeString = exception.Type.ToString();

            // Store the exception message and type in session
            context.Session.SetString("UserFriendlyMessage", exception.Message);
            context.Session.SetString("UserFriendlyMessageType", messageTypeString);

            // Check if the request is an AJAX request
            bool isAjaxRequest = context.Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (isAjaxRequest)
            {
                // For AJAX requests, return a JSON response with the message
                context.Response.StatusCode = 200; // Use 200 instead of error code since this is user-friendly
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync($"{{\"userFriendlyMessage\": \"{exception.Message}\", \"messageType\": \"{messageTypeString}\"}}");
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