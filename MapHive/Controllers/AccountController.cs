using MapHive.Models;
using MapHive.Models.Exceptions;
using MapHive.Repositories;
using MapHive.Services;
using MapHive.Utilities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MapHive.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserRepository _userRepository;
        private readonly LogManager _logManager;
        private readonly IMapLocationRepository _mapLocationRepository;

        public AccountController(IAuthService authService, IHttpContextAccessor httpContextAccessor,
            IUserRepository userRepository, LogManager logManager, IMapLocationRepository mapLocationRepository)
        {
            this._authService = authService;
            this._httpContextAccessor = httpContextAccessor;
            this._userRepository = userRepository;
            this._logManager = logManager;
            this._mapLocationRepository = mapLocationRepository;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return this.User.Identity?.IsAuthenticated == true ? this.RedirectToAction("Index", "Home") : this.View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginRequest model)
        {
            if (!this.ModelState.IsValid)
            {
                return this.View(model);
            }

            AuthResponse response = await this._authService.LoginAsync(model);

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

                await this.HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    authProperties);

                return this.RedirectToAction("Index", "Home");
            }

            this.ModelState.AddModelError("", response.Message);
            return this.View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return this.User.Identity?.IsAuthenticated == true ? this.RedirectToAction("Index", "Home") : this.View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterRequest model)
        {
            if (!this.ModelState.IsValid)
            {
                return this.View(model);
            }

            // Get client IP address
            string? ipAddress = this._httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = "0.0.0.0";
            }

            // Get or simulate client MAC address
            string macAddress = NetworkUtilities.GetMacAddressFromIp(ipAddress);

            AuthResponse response = await this._authService.RegisterAsync(model, ipAddress, macAddress);

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

                await this.HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    authProperties);

                return this.RedirectToAction("Index", "Home");
            }

            this.ModelState.AddModelError("", response.Message);
            return this.View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await this.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return this.RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            string? userId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null || !int.TryParse(userId, out int id))
            {
                this.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).GetAwaiter().GetResult();
                throw new OrangeUserException("Your session is invalid, login again.");
            }

            User? user = this._userRepository.GetUserById(id);
            if (user == null)
            {
                this.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).GetAwaiter().GetResult();
                throw new OrangeUserException("Your session is invalid, login again.");
            }

            // Get the user's places
            IEnumerable<MapLocation> userLocations = await this._mapLocationRepository.GetLocationsByUserIdAsync(id);

            ProfileViewModel model = new()
            {
                Username = user.Username,
                IsTrusted = user.IsTrusted,
                RegistrationDate = user.RegistrationDate,
                UserLocations = userLocations
            };

            return this.View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ChangeUsername(ChangeUsernameViewModel model)
        {
            if (!this.ModelState.IsValid)
            {
                ProfileViewModel? profileModel = this.GetCurrentUserProfile();
                return profileModel == null ? this.RedirectToAction("Login") : this.View("Profile", profileModel);
            }

            string? userId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null || !int.TryParse(userId, out int id))
            {
                return this.RedirectToAction("Login");
            }

            User? user = this._userRepository.GetUserById(id);
            if (user == null)
            {
                return this.RedirectToAction("Login");
            }

            // Check if the username already exists
            if (this._userRepository.CheckUsernameExists(model.NewUsername) && !user.Username.Equals(model.NewUsername, StringComparison.OrdinalIgnoreCase))
            {
                this.ModelState.AddModelError("NewUsername", "Username already exists");
                ProfileViewModel? profileModel = this.GetCurrentUserProfile();
                return profileModel == null ? this.RedirectToAction("Login") : this.View("Profile", profileModel);
            }

            // Update the username
            user.Username = model.NewUsername;
            this._userRepository.UpdateUser(user);

            // Update user claims
            List<Claim> claims = new()
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("IsTrusted", user.IsTrusted.ToString()),
                new Claim(ClaimTypes.Role, user.IsAdmin ? "Admin" : "User")
            };

            ClaimsIdentity identity = new(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            ClaimsPrincipal principal = new(identity);

            AuthenticationProperties authProperties = new()
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };

            await this.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await this.HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                authProperties);

            this.TempData["SuccessMessage"] = "Username changed successfully";
            return this.RedirectToAction("Profile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if (!this.ModelState.IsValid)
            {
                ProfileViewModel? profileModel = this.GetCurrentUserProfile();
                return profileModel == null ? this.RedirectToAction("Login") : this.View("Profile", profileModel);
            }

            string? userId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null || !int.TryParse(userId, out int id))
            {
                return this.RedirectToAction("Login");
            }

            User? user = this._userRepository.GetUserById(id);
            if (user == null)
            {
                return this.RedirectToAction("Login");
            }

            // Verify current password
            if (!this._authService.VerifyPassword(model.CurrentPassword, user.PasswordHash))
            {
                this.ModelState.AddModelError("CurrentPassword", "Current password is incorrect");
                ProfileViewModel? profileModel = this.GetCurrentUserProfile();
                return profileModel == null ? this.RedirectToAction("Login") : this.View("Profile", profileModel);
            }

            // Update password
            user.PasswordHash = this._authService.HashPassword(model.NewPassword);
            this._userRepository.UpdateUser(user);

            this.TempData["SuccessMessage"] = "Password changed successfully";
            return this.RedirectToAction("Profile");
        }

        private ProfileViewModel? GetCurrentUserProfile()
        {
            string? userId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null || !int.TryParse(userId, out int id))
            {
                return null;
            }

            User? user = this._userRepository.GetUserById(id);
            return user == null
                ? null
                : new ProfileViewModel
                {
                    Username = user.Username,
                    IsTrusted = user.IsTrusted,
                    RegistrationDate = user.RegistrationDate
                };
        }
    }
}