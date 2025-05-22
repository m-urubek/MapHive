namespace MapHive.Services;

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

    public async Task<int> LogAsync(
        LogSeverity severity,
        string message,
        Exception? exception = null,
        string? source = null,
        string? additionalData = null)
    {
        int? accountId = _userContextService.IsAuthenticated ? _userContextService.AccountIdOrThrow : null;
        string requestPath = _requestContextService.IsInRequest ? _requestContextService.RequestPath : "not in request";
        return await _logManagerSingleton.LogAsync(severity: severity, message: message, exception: exception, source: source, additionalData: additionalData, accountId: accountId, requestPath: requestPath);
    }
}
