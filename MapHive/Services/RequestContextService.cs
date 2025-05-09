using MapHive.Models.Exceptions;
using System.Security.Claims;

namespace MapHive.Services
{
    /// <summary>
    /// Implementation of IUserContextService providing user details from HttpContext.
    /// </summary>
    public class RequestContextService : IRequestContextService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RequestContextService(IHttpContextAccessor httpContextAccessor)
        {
            this._httpContextAccessor = httpContextAccessor;
        }

        public string RequestPath => this._httpContextAccessor.HttpContext?.Request.Path.ToString() ?? throw new Exception("Not in request");

        public string IpAddress =>
            (this._httpContextAccessor.HttpContext ?? throw new Exception("Not in request"))
                .Connection.RemoteIpAddress?.ToString()
            ?? throw new WarningException("Unable to retrieve IP address");

        public bool IsRequestAjax => this._httpContextAccessor.HttpContext?.Request.Headers["X-Requested-With"].ToString() == "XMLHttpRequest";

        public bool IsInRequest => this._httpContextAccessor.HttpContext != null;

        public string? Referer
        {
            get
            {
                if (this._httpContextAccessor.HttpContext == null)
                {
                    throw new Exception("Not in request");
                }
                else
                {
                    string? referer = this._httpContextAccessor.HttpContext?.Request.Headers["Referer"].ToString();
                    return string.IsNullOrWhiteSpace(referer) ? null : referer;
                }
            }
        }

    }
}