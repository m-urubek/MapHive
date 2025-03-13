using MapHive.Models;
using MapHive.Services;
using MapHive.Utilities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MapHive.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly LogManager _logManager;

        public AuthController(IAuthService authService, IHttpContextAccessor httpContextAccessor, LogManager logManager)
        {
            this._authService = authService;
            this._httpContextAccessor = httpContextAccessor;
            this._logManager = logManager;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!this.ModelState.IsValid)
            {
                return this.BadRequest(ApiResponse<object>.ErrorResponse("Invalid registration data"));
            }

            // Get client IP address
            string? ipAddress = this._httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = "0.0.0.0";
            }

            // Get or simulate client MAC address
            string macAddress = NetworkUtilities.GetMacAddressFromIp(ipAddress);

            // Process registration
            AuthResponse response = await this._authService.RegisterAsync(request, ipAddress, macAddress);

            if (response.Success && response.User != null)
            {
                // Create claims
                List<Claim> claims = new()
                {
                    new Claim(ClaimTypes.Name, response.User.Username),
                    new Claim(ClaimTypes.NameIdentifier, response.User.Id.ToString()),
                    new Claim("IsTrusted", response.User.IsTrusted.ToString()),
                    new Claim(ClaimTypes.Role, response.User.IsAdmin ? "Admin" : "User")
                };

                ClaimsIdentity identity = new(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                ClaimsPrincipal principal = new(identity);

                AuthenticationProperties authProperties = new()
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                };

                // Sign in the user
                await this.HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    authProperties);

                return this.Ok(ApiResponse<User>.SuccessResponse(response.User, response.Message));
            }

            return this.BadRequest(ApiResponse<object>.ErrorResponse(response.Message));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!this.ModelState.IsValid)
            {
                return this.BadRequest(ApiResponse<object>.ErrorResponse("Invalid login data"));
            }

            // Process login
            AuthResponse response = await this._authService.LoginAsync(request);

            if (response.Success && response.User != null)
            {
                // Create claims
                List<Claim> claims = new()
                {
                    new Claim(ClaimTypes.Name, response.User.Username),
                    new Claim(ClaimTypes.NameIdentifier, response.User.Id.ToString()),
                    new Claim("IsTrusted", response.User.IsTrusted.ToString()),
                    new Claim(ClaimTypes.Role, response.User.IsAdmin ? "Admin" : "User")
                };

                ClaimsIdentity identity = new(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                ClaimsPrincipal principal = new(identity);

                AuthenticationProperties authProperties = new()
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                };

                // Sign in the user
                await this.HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    authProperties);

                return this.Ok(ApiResponse<User>.SuccessResponse(response.User, response.Message));
            }

            return this.BadRequest(ApiResponse<object>.ErrorResponse(response.Message));
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await this.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return this.Ok(ApiResponse<object>.SuccessResponse(new { }, "Logout successful"));
        }
    }
}