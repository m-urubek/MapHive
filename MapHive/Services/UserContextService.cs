namespace MapHive.Services
{
    using System.Security.Claims;
    using MapHive.Models.Exceptions;

    /// <summary>
    /// Implementation of IUserContextService providing user details from HttpContext.
    /// </summary>
    public class UserContextService(IHttpContextAccessor httpContextAccessor) : IUserContextService
    {
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

        private HttpContext HttpContext => _httpContextAccessor.HttpContext ?? throw new Exception("Not in request");

        public void EnsureAuthenticated()
        {
            if (!IsAuthenticated)
            {
                throw new PublicWarningException("Not authenticated");
            }
        }

        public int UserIdRequired
        {
            get
            {
                EnsureAuthenticated();
                Claim? userIdClaim = HttpContext.User.FindFirst(type: ClaimTypes.NameIdentifier);
                return userIdClaim != null && int.TryParse(s: userIdClaim.Value, result: out int userId) ? userId : throw new Exception("Unable to retreive user claims");
            }
        }

        public string UsernameRequired
        {
            get
            {
                EnsureAuthenticated();
                return HttpContext.User.Identity?.Name ?? throw new Exception("Unable to retreive HttpContext.User.Identity");
            }
        }

        public bool IsAuthenticated => HttpContext?.User?.Identity?.IsAuthenticated == true;

        public bool IsAdminRequired
        {
            get
            {
                EnsureAuthenticated();
                return HttpContext.User.IsInRole(role: "Admin");
            }
        }

        public bool IsAuthenticatedAndAdmin => IsAuthenticated && IsAdminRequired;
    }
}
