using MapHive.Models.Enums;
using MapHive.Models.RepositoryModels;
using MapHive.Repositories;
using MapHive.Singletons;
using Newtonsoft.Json;
using System.Text;

namespace MapHive.Services
{
    public class LogManagerService : ILogManagerService
    {
        private readonly ILogManagerSingleton _logManagerSingleton;
        private readonly IRequestContextService _requestContextService;
        private readonly IUserContextService _userContextService;

        public LogManagerService(
            ILogManagerSingleton logManagerSingleton,
            IRequestContextService requestContextService,
            IUserContextService userContextService)
        {
            this._logManagerSingleton = logManagerSingleton;
            this._requestContextService = requestContextService;
            this._userContextService = userContextService;
        }

        public void Log(
            LogSeverity severity,
            string message,
            Exception? exception = null,
            string? source = null,
            string? additionalData = null)
        {
            this._logManagerSingleton.Log(severity, message, exception, source, additionalData, this._userContextService.UserId, this._requestContextService.RequestPath);
        }
    }
}