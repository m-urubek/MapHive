using MapHive.Models.Enums;
using MapHive.Models.Exceptions;
using MapHive.Services;
using MapHive.Singletons;
using Microsoft.Extensions.DependencyInjection;

namespace MapHive.Middleware
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfigurationSingleton _configurationSingleton;

        public ErrorHandlingMiddleware(RequestDelegate next, IConfigurationSingleton config)
        {
            this._next = next;
            this._configurationSingleton = config;
        }

        public async Task InvokeAsync(HttpContext context, ILogManagerService logManager)
        {
            // Resolve scoped services from the request
            var userFriendlyExceptionService = context.RequestServices.GetRequiredService<IUserFriendlyExceptionService>();
            var requestContextService = context.RequestServices.GetRequiredService<IRequestContextService>();
            try
            {
                await this._next(context);
            }
            catch (UserFriendlyExceptionBase ex)
            {
                // For user-friendly exceptions, don't log them as errors but show to the user
                await this.HandleUserFriendlyExceptionAsync(context, ex, userFriendlyExceptionService, requestContextService);

                // Don't re-throw the exception since we've handled it by showing a friendly message
            }
            catch (WarningException ex)
            {
                logManager.Log(
                    LogSeverity.Warning,
                    ex.Message,
                    ex,
                    nameof(ErrorHandlingMiddleware),
                    $"{{\"path\": \"{context.Request.Path}\", \"method\": \"{context.Request.Method}\"}}"
                );
                await this.HandleUserFriendlyExceptionAsync(context, new OrangeUserException(ex.Message), userFriendlyExceptionService, requestContextService);
            }
            catch (Exception ex)
            {
                try
                {
                    if (await this._configurationSingleton.GetDevelopmentModeAsync())
                    {
                        //TODO after admin panel is done, display link to some page in admin panel displaying details of the exception
                        await this.HandleUserFriendlyExceptionAsync(context, new RedUserException(ex.ToString()), userFriendlyExceptionService, requestContextService);
                    }
                    else
                    {
                        // Show a generic error message to the user
                        await this.HandleUserFriendlyExceptionAsync(context, new RedUserException("Internal Server Error!"), userFriendlyExceptionService, requestContextService);
                    }
                }
                catch { } // Don't let UI message display issues prevent error logging

                logManager.Log(
                    severity: LogSeverity.Critical,
                    message: ex.Message,
                    source: nameof(ErrorHandlingMiddleware),
                    exception: ex,
                    additionalData: $"{{\"path\": \"{context.Request.Path}\", \"method\": \"{context.Request.Method}\"}}"
                );
            }
        }

        private async Task HandleUserFriendlyExceptionAsync(HttpContext context, UserFriendlyExceptionBase exception,
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
                await context.Response.WriteAsync($"{{\"userFriendlyMessage\": \"{exception.Message}\", \"messageType\": \"{userFriendlyExceptionService.Type}\"}}");
            }
            else
            {
                // For regular requests, redirect back to the same page (or referrer if available)
                context.Response.Redirect(requestContextService.Referer ?? context.Request.Path);
            }
        }
    }
}