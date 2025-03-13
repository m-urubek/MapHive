using MapHive.Services;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace MapHive.Middleware
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public ErrorHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, LogManager logManager)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                HandleExceptionAsync(context, ex, logManager);
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
    }
} 