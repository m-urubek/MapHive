namespace MapHive.Services;

using System.Security.Claims;
using MapHive.Models.Exceptions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

/// <summary>
/// Implementation of IUserContextService providing user details from HttpContext.
/// </summary>
public class UserContextService(IHttpContextAccessor httpContextAccessor) : IUserContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    private HttpContext HttpContext => _httpContextAccessor.HttpContext ?? throw new Exception("Not in request");

    public int AccountIdOrThrow
    {
        get
        {
            EnsureAuthenticated();
            Claim? accountIdClaim = HttpContext.User.FindFirst(type: ClaimTypes.NameIdentifier);
            return accountIdClaim != null && int.TryParse(s: accountIdClaim.Value, result: out int accountId) ? accountId : throw new Exception("Unable to retreive user claims");
        }
    }

    public string UsernameOrThrow
    {
        get
        {
            EnsureAuthenticated();
            return HttpContext.User.Identity?.Name ?? throw new Exception("Unable to retreive HttpContext.User.Identity");
        }
    }

    public bool IsAuthenticated => HttpContext?.User?.Identity?.IsAuthenticated == true;

    public bool IsAdminOrThrow
    {
        get
        {
            EnsureAuthenticated();
            return HttpContext.User.IsInRole(role: "Admin");
        }
    }

    public bool IsAuthenticatedAndAdmin => IsAuthenticated && IsAdminOrThrow;

    public void EnsureAuthenticated()
    {
        if (!IsAuthenticated)
        {
            throw new PublicWarningException("Not authenticated");
        }
    }

    public void EnsureAuthenticatedAndAdmin()
    {
        if (!IsAuthenticatedAndAdmin)
        {
            throw new PublicErrorException("Not authorized");
        }
    }

    public async Task SetClaim(string claimKey, string claimValue)
    {
        // Ensure user is authenticated
        EnsureAuthenticated();

        // Modify claims
        ClaimsIdentity identity = HttpContext.User.Identity as ClaimsIdentity ?? throw new Exception("User identity is not a ClaimsIdentity");
        Claim? existingClaim = identity.FindFirst(claimKey);
        if (existingClaim != null)
            identity.RemoveClaim(existingClaim);
        identity.AddClaim(new Claim(claimKey, claimValue));

        // Re-issue authentication cookie with updated claims
        ClaimsPrincipal principal = new(identity);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = true });
    }
}
