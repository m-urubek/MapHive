namespace MapHive.Services
{
    using MapHive.Models.Exceptions;

    /// <summary>
    /// Implementation of IUserContextService providing user details from HttpContext.
    /// </summary>
    public class RequestContextService(IHttpContextAccessor httpContextAccessor) : IRequestContextService
    {
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

        public string RequestPath => _httpContextAccessor.HttpContext?.Request.Path.ToString() ?? throw new Exception("Not in request");

        public string IpAddress =>
            (_httpContextAccessor.HttpContext ?? throw new Exception("Not in request"))
                .Connection.RemoteIpAddress?.ToString()
            ?? throw new WarningException("Unable to retrieve IP address");

        public bool IsRequestAjax => _httpContextAccessor.HttpContext?.Request.Headers.XRequestedWith.ToString() == "XMLHttpRequest";

        public bool IsInRequest => _httpContextAccessor.HttpContext != null;

        public string? Referer
        {
            get
            {
                if (_httpContextAccessor.HttpContext == null)
                {
                    throw new Exception("Not in request");
                }
                else
                {
                    string? referer = _httpContextAccessor.HttpContext?.Request.Headers.Referer.ToString();
                    return string.IsNullOrWhiteSpace(value: referer) ? null : referer;
                }
            }
        }
    }
}