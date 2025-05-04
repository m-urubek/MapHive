using System.Security.Claims;

namespace MapHive.Services
{
    /// <summary>
    /// Implementation of IUserContextService providing user details from HttpContext.
    /// </summary>
    public class UserContextService : IUserContextService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserContextService(IHttpContextAccessor httpContextAccessor)
        {
            this._httpContextAccessor = httpContextAccessor;
        }

        private HttpContext? HttpContext => this._httpContextAccessor.HttpContext;

        public int? UserId
        {
            get
            {
                if (!this.IsAuthenticated || this.HttpContext == null)
                {
                    return null;
                }

                Claim? userIdClaim = this.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
                return userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId) ? userId : null;
            }
        }

        public string? Username => !this.IsAuthenticated || this.HttpContext == null ? null : (this.HttpContext.User.Identity?.Name);

        public bool IsAuthenticated => this.HttpContext?.User?.Identity?.IsAuthenticated == true;
    }
}