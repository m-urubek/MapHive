namespace MapHive.Middleware
{
    using MapHive.Models.Enums;
    using MapHive.Models.Exceptions;
    using MapHive.Services;
    using Microsoft.Extensions.DependencyInjection;

    public class ErrorHandlingMiddleware(RequestDelegate next)
    {
        public async Task InvokeAsync(HttpContext context, ILogManagerService logManagerService, IConfigurationService configurationService, IUserContextService userContextService)
        {
            // Resolve scoped services from the request
            IUserFriendlyExceptionService userFriendlyExceptionService = context.RequestServices.GetRequiredService<IUserFriendlyExceptionService>();
            IRequestContextService requestContextService = context.RequestServices.GetRequiredService<IRequestContextService>();
            try
            {
                await next(context: context);
            }
            catch (UserFriendlyExceptionBase ex)
            {
                // For user-friendly exceptions, don't log them as errors but show to the user
                await HandleUserFriendlyExceptionAsync(context: context, exception: ex, userFriendlyExceptionService: userFriendlyExceptionService, requestContextService: requestContextService);

                // Don't re-throw the exception since we've handled it by showing a friendly message
            }
            catch (PublicWarningException ex)
            {
                _ = logManagerService.LogAsync(
                    severity: LogSeverity.Warning,
                    message: ex.Message,
                    exception: ex,
                    source: nameof(ErrorHandlingMiddleware),
                    additionalData: $"{{\"path\": \"{context.Request.Path}\", \"method\": \"{context.Request.Method}\"}}"
                );
                await HandleUserFriendlyExceptionAsync(context: context, exception: new OrangeUserException(ex.Message), userFriendlyExceptionService: userFriendlyExceptionService, requestContextService: requestContextService);
            }
            catch (PublicErrorException ex)
            {
                _ = logManagerService.LogAsync(
                    severity: LogSeverity.Error,
                    message: ex.Message,
                    exception: ex,
                    source: nameof(ErrorHandlingMiddleware),
                    additionalData: $"{{\"path\": \"{context.Request.Path}\", \"method\": \"{context.Request.Method}\"}}"
                );
                await HandleUserFriendlyExceptionAsync(context: context, exception: new RedUserException(ex.Message), userFriendlyExceptionService: userFriendlyExceptionService, requestContextService: requestContextService);
            }
            catch (UnauthorizedAccessException ex)
            {
                _ = logManagerService.LogAsync(
                    severity: LogSeverity.Error,
                    message: "User is not authorized to access this resource",
                    exception: ex,
                    source: nameof(ErrorHandlingMiddleware),
                    additionalData: $"{{\"path\": \"{context.Request.Path}\", \"method\": \"{context.Request.Method}\"}}"
                );
                await HandleUserFriendlyExceptionAsync(context: context, exception: new RedUserException("Unauthorized"), userFriendlyExceptionService: userFriendlyExceptionService, requestContextService: requestContextService);
            }
            catch (Exception ex)
            {
                try
                {
                    if (await configurationService.GetDevelopmentModeAsync() || (userContextService.IsAuthenticated && userContextService.IsAdminRequired))
                    {
                        int logId = await logManagerService.LogAsync(
                            severity: LogSeverity.Critical,
                            message: ex.Message,
                            exception: ex,
                            source: nameof(ErrorHandlingMiddleware),
                            additionalData: $"{{\"path\": \"{context.Request.Path}\", \"method\": \"{context.Request.Method}\"}}"
                        );
                        string errorMessage = $"<a href=\"/Display/Item?tableName=Logs&id={logId}\">View log details</a><br/>{ex}";
                        await HandleUserFriendlyExceptionAsync(context: context, exception: new RedUserException(errorMessage), userFriendlyExceptionService: userFriendlyExceptionService, requestContextService: requestContextService);
                    }
                    else
                    {
                        _ = logManagerService.LogAsync(
                            severity: LogSeverity.Critical,
                            message: ex.Message,
                            exception: ex,
                            source: nameof(ErrorHandlingMiddleware),
                            additionalData: $"{{\"path\": \"{context.Request.Path}\", \"method\": \"{context.Request.Method}\"}}"
                        );
                        // Show a generic error message to the user
                        await HandleUserFriendlyExceptionAsync(context: context, exception: new RedUserException("Internal Server Error!"), userFriendlyExceptionService: userFriendlyExceptionService, requestContextService: requestContextService);
                    }
                }
                catch { } // Don't let UI message display issues prevent error logging
            }
        }

        private static async Task HandleUserFriendlyExceptionAsync(HttpContext context, UserFriendlyExceptionBase exception,
            IUserFriendlyExceptionService userFriendlyExceptionService, IRequestContextService requestContextService)
        {
            // Store the exception message and type in session
            userFriendlyExceptionService.Message = exception.Message;
            userFriendlyExceptionService.Type = exception.Type.ToString();

            // Check if the request is an AJAX request
            if (requestContextService.IsRequestAjax)
            {
                // For AJAX requests, return a JSON response with the message
                context.Response.StatusCode = 200; // Use 200 instead of error code since this is user-friendly
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(text: $"{{\"userFriendlyMessage\": \"{exception.Message}\", \"messageType\": \"{userFriendlyExceptionService.Type}\"}}");
            }
            else
            {
                // For regular requests, redirect back to the same page (or referrer if available)
                // The session persists across requests via a cookie, so the next request's
                // view component will pick up the message from session and display the popup.
                context.Response.Redirect(location: requestContextService.Referer ?? context.Request.Path);
            }
        }
    }
}
