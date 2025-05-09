namespace MapHive.Services
{
    using MapHive.Models.Enums;
    using MapHive.Singletons;

    public class LogManagerService(
        ILogManagerSingleton logManagerSingleton,
        IRequestContextService requestContextService,
        IUserContextService userContextService) : ILogManagerService
    {
        private readonly ILogManagerSingleton _logManagerSingleton = logManagerSingleton;
        private readonly IRequestContextService _requestContextService = requestContextService;
        private readonly IUserContextService _userContextService = userContextService;

        public void Log(
            LogSeverity severity,
            string message,
            Exception? exception = null,
            string? source = null,
            string? additionalData = null)
        {
            _logManagerSingleton.Log(severity: severity, message: message, exception: exception, source: source, additionalData: additionalData, userId: _userContextService.UserId, requestPath: _requestContextService.RequestPath);
        }
    }
}