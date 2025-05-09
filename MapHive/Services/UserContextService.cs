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

        private void EnsureAuthenticated()
        {
            if (!IsAuthenticated)
            {
                throw new WarningException("Not authenticated");
            }
        }

        public int UserId
        {
            get
            {
                EnsureAuthenticated();
                Claim? userIdClaim = HttpContext.User.FindFirst(type: ClaimTypes.NameIdentifier);
                return userIdClaim != null && int.TryParse(s: userIdClaim.Value, result: out int userId) ? userId : throw new Exception("Unable to retreive user claims");
            }
        }

        public string Username
        {
            get
            {
                EnsureAuthenticated();
                return HttpContext.User.Identity?.Name ?? throw new Exception("Unable to retreive HttpContext.User.Identity");
            }
        }

        public bool IsAuthenticated => HttpContext?.User?.Identity?.IsAuthenticated == true;
    }
}