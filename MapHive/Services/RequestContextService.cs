namespace MapHive.Services
{
    using MapHive.Models.Exceptions;
    using MapHive.Utilities;

    /// <summary>
    /// Implementation of IUserContextService providing user details from HttpContext.
    /// </summary>
    public class RequestContextService(IHttpContextAccessor httpContextAccessor) : IRequestContextService
    {
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

        public string RequestPath => _httpContextAccessor.HttpContext?.Request.Path.ToString() ?? throw new Exception("Not in request");

        private string? _hashedIpAddress;
        public string HashedIpAddress
        {
            get
            {
                if (_hashedIpAddress is null)
                {
                    string ipAddress = (_httpContextAccessor.HttpContext ?? throw new Exception("Not in request"))
                        .Connection.RemoteIpAddress?.ToString()
                    ?? throw new PublicWarningException("Unable to retrieve IP address");
                    _hashedIpAddress = HashingUtility.HashIpAddress(ipAddress: ipAddress);
                }
                return _hashedIpAddress;
            }
        }

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