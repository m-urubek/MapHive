using MapHive.Models.Exceptions;
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

        private HttpContext HttpContext => this._httpContextAccessor.HttpContext ?? throw new Exception("Not in request");

        private void EnsureAuthenticated()
        {
            if (!this.IsAuthenticated)
            {
                throw new WarningException("Not authenticated");
            }
        }

        public int UserId
        {
            get
            {
                this.EnsureAuthenticated();
                Claim? userIdClaim = this.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
                return userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId) ? userId : throw new Exception("Unable to retreive user claims");
            }
        }

        public string Username
        {
            get
            {
                this.EnsureAuthenticated();
                return this.HttpContext.User.Identity?.Name ?? throw new Exception("Unable to retreive HttpContext.User.Identity");
            }
        }

        public bool IsAuthenticated => this.HttpContext?.User?.Identity?.IsAuthenticated == true;
    }
}