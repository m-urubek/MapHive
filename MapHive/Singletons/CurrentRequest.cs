using MapHive.Repositories;
using MapHive.Services;
using reCAPTCHA.AspNetCore;
using System.Security.Claims;

namespace MapHive.Singletons
{
    /// <summary>
    /// Static accessor for the current request's services, repositories and user context.
    /// </summary>
    public static class CurrentRequest
    {
        private static IServiceProvider? _rootServiceProvider;
        private static IHttpContextAccessor? _httpContextAccessor;

        /// <summary>
        /// Initializes the CurrentRequest class with the application's service provider
        /// </summary>
        public static void Initialize(IServiceProvider serviceProvider)
        {
            _rootServiceProvider = serviceProvider;
            _httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
        }

        /// <summary>
        /// Gets the current scope's service provider, or falls back to the root provider if outside a request
        /// </summary>
        private static IServiceProvider CurrentScope =>
            _httpContextAccessor?.HttpContext?.RequestServices ??
            _rootServiceProvider ??
            throw new InvalidOperationException("CurrentRequest has not been initialized");

        /// <summary>
        /// Gets a service of the specified type from the current scope
        /// </summary>
        private static T GetService<T>() where T : notnull
        {
            return CurrentScope.GetRequiredService<T>();
        }

        // Repositories
        public static IMapLocationRepository MapRepository => GetService<IMapLocationRepository>();
        public static IUserRepository UserRepository => GetService<IUserRepository>();
        public static IConfigurationRepository ConfigRepository => GetService<IConfigurationRepository>();
        public static IReviewRepository ReviewRepository => GetService<IReviewRepository>();
        public static IDiscussionRepository DiscussionRepository => GetService<IDiscussionRepository>();

        // Services
        public static IAuthService AuthService => GetService<IAuthService>();
        public static LogManagerService LogManager => GetService<LogManagerService>();
        public static RecaptchaService RecaptchaService => GetService<RecaptchaService>();
        public static IHttpContextAccessor HttpContext => GetService<IHttpContextAccessor>();
        /// <summary>
        /// Gets the current user's ID if authenticated, otherwise returns null
        /// </summary>
        public static int? CurrentUserId
        {
            get
            {
                HttpContext? httpContext = _httpContextAccessor?.HttpContext;
                if (httpContext?.User?.Identity?.IsAuthenticated != true)
                {
                    return null;
                }

                Claim? userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
                return userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId) ? userId : null;
            }
        }

        /// <summary>
        /// Gets the current user's username if authenticated, otherwise returns null
        /// </summary>
        public static string? Username
        {
            get
            {
                HttpContext? httpContext = _httpContextAccessor?.HttpContext;
                return httpContext?.User?.Identity?.IsAuthenticated != true ? null : httpContext.User.Identity.Name;
            }
        }

        /// <summary>
        /// Determines if the current user is authenticated
        /// </summary>
        public static bool IsAuthenticated => _httpContextAccessor?.HttpContext?.User?.Identity?.IsAuthenticated == true;

        public static string? RequestPath => _httpContextAccessor?.HttpContext?.Request?.Path;
    }
}