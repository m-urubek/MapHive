namespace MapHive.Middleware
{
    using MapHive.Models.Enums;
    using MapHive.Models.Exceptions;
    using MapHive.Services;
    using MapHive.Singletons;
    using Microsoft.Extensions.DependencyInjection;

    public class ErrorHandlingMiddleware(RequestDelegate next)
    {
        public async Task InvokeAsync(HttpContext context, ILogManagerService logManager, IConfigurationService configurationService)
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
            catch (WarningException ex)
            {
                logManager.Log(
                    severity: LogSeverity.Warning,
                    message: ex.Message,
                    exception: ex,
                    source: nameof(ErrorHandlingMiddleware),
                    additionalData: $"{{\"path\": \"{context.Request.Path}\", \"method\": \"{context.Request.Method}\"}}"
                );
                await HandleUserFriendlyExceptionAsync(context: context, exception: new OrangeUserException(ex.Message), userFriendlyExceptionService: userFriendlyExceptionService, requestContextService: requestContextService);
            }
            catch (Exception ex)
            {
                try
                {
                    if (await configurationService.GetDevelopmentModeAsync())
                    {
                        //TODO after admin panel is done, display link to some page in admin panel displaying details of the exception
                        await HandleUserFriendlyExceptionAsync(context: context, exception: new RedUserException(ex.ToString()), userFriendlyExceptionService: userFriendlyExceptionService, requestContextService: requestContextService);
                    }
                    else
                    {
                        // Show a generic error message to the user
                        await HandleUserFriendlyExceptionAsync(context: context, exception: new RedUserException("Internal Server Error!"), userFriendlyExceptionService: userFriendlyExceptionService, requestContextService: requestContextService);
                    }
                }
                catch { } // Don't let UI message display issues prevent error logging

                logManager.Log(
                    severity: LogSeverity.Critical,
                    message: ex.Message,
                    exception: ex,
                    source: nameof(ErrorHandlingMiddleware),
                    additionalData: $"{{\"path\": \"{context.Request.Path}\", \"method\": \"{context.Request.Method}\"}}"
                );
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
                context.Response.Redirect(location: requestContextService.Referer ?? context.Request.Path);
            }
        }
    }
}