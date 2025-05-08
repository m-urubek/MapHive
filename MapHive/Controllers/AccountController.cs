using AutoMapper;
using MapHive.Models;
using MapHive.Models.Enums;
using MapHive.Models.Exceptions;
using MapHive.Models.ViewModels;
using MapHive.Services;
using MapHive.Singletons;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using reCAPTCHA.AspNetCore;
using System.Security.Claims;

namespace MapHive.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAccountService _accountService;
        private readonly IAdminService _adminService;
        private readonly IProfileService _profileService;
        private readonly ILogManagerSingleton _logManager;
        private readonly IRecaptchaService _recaptchaService;
        private readonly RecaptchaSettings _recaptchaSettings;
        private readonly IConfigurationSingleton _configSingleton;
        private readonly IUserContextService _userContextService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMapper _mapper;

        public AccountController(
            IAccountService accountService,
            IAdminService adminService,
            IProfileService profileService,
            ILogManagerSingleton logManager,
            IRecaptchaService recaptchaService,
            IConfigurationSingleton configSingleton,
            IUserContextService userContextService,
            IHttpContextAccessor httpContextAccessor,
            IOptions<RecaptchaSettings> recaptchaOptions,
            IMapper mapper)
        {
            this._accountService = accountService;
            this._adminService = adminService;
            this._profileService = profileService;
            this._logManager = logManager;
            this._recaptchaService = recaptchaService;
            this._recaptchaSettings = recaptchaOptions.Value;
            this._configSingleton = configSingleton;
            this._userContextService = userContextService;
            this._httpContextAccessor = httpContextAccessor;
            this._mapper = mapper;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return this._userContextService.IsAuthenticated ? this.RedirectToAction("Index", "Home") : this.View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginRequest loginRequest)
        {
            if (!this.ModelState.IsValid)
            {
                return this.View(loginRequest);
            }

            try
            {
                _ = await this._accountService.LoginAsync(loginRequest);
                return this.RedirectToAction("Index", "Home");
            }
            catch (UserFriendlyExceptionBase ex)
            {
                this.ModelState.AddModelError("", ex.Message);
                return this.View(loginRequest);
            }
            catch (Exception ex)
            {
                this._logManager.Error($"Unexpected error during login for {loginRequest.Username}.", exception: ex);
                this.ModelState.AddModelError("", "An unexpected error occurred during login. Please try again later.");
                return this.View(loginRequest);
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            return this._userContextService.IsAuthenticated ? this.RedirectToAction("Index", "Home") : this.View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterRequest registerRequest)
        {
            RecaptchaSettings recaptchaSettings = this._recaptchaSettings;
            bool devMode = await this._configSingleton.GetDevelopmentModeAsync();
            bool isUsingTestKeys = devMode && recaptchaSettings.SiteKey == "6LeIxAcTAAAAAJcZVRqyHh71UMIEGNQ_MXjiZKhI";

            if (string.IsNullOrEmpty(registerRequest.RecaptchaResponse) && this.Request.Form.ContainsKey("g-recaptcha-response"))
            {
                registerRequest.RecaptchaResponse = this.Request.Form["g-recaptcha-response"];
                _ = this.ModelState.Remove("RecaptchaResponse");
            }

            if (!this.ModelState.IsValid)
            {
                return this.View(registerRequest);
            }

            if (!isUsingTestKeys)
            {
                RecaptchaResponse? validationResponse = null;
                if (!string.IsNullOrEmpty(registerRequest.RecaptchaResponse))
                {
                    validationResponse = await this._recaptchaService.Validate(registerRequest.RecaptchaResponse);
                }

                if (validationResponse == null || !validationResponse.success)
                {
                    this._logManager.Warning("reCAPTCHA validation failed for user registration attempt.");
                    this.ModelState.AddModelError("RecaptchaResponse", "reCAPTCHA verification failed. Please try again.");
                    return this.View(registerRequest);
                }
            }
            else if (!string.IsNullOrEmpty(registerRequest.RecaptchaResponse))
            {
                this._logManager.Information("Accepting reCAPTCHA test response during registration.");
            }
            else
            {
                this.ModelState.AddModelError("RecaptchaResponse", "reCAPTCHA response missing (Test Mode).");
                return this.View(registerRequest);
            }

            try
            {
                _ = await this._accountService.RegisterAsync(registerRequest);
                return this.RedirectToAction("Index", "Home");
            }
            catch (UserFriendlyExceptionBase ex)
            {
                this.ModelState.AddModelError("", ex.Message);
                return this.View(registerRequest);
            }
            catch (Exception ex)
            {
                this._logManager.Error($"Unexpected error during registration for {registerRequest.Username}.", exception: ex);
                this.ModelState.AddModelError("", "An unexpected error occurred during registration. Please try again later.");
                return this.View(registerRequest);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await this._accountService.LogoutAsync();
            return this.RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [Authorize]
        public IActionResult Profile()
        {
            return this.RedirectToAction("PrivateProfile");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> PrivateProfile()
        {
            int userId = this._userContextService.UserId ?? 0;
            PrivateProfileViewModel? viewModel = await this._profileService.GetPrivateProfileAsync(userId);
            return viewModel == null ? this.RedirectToAction("Login") : this.View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> PublicProfile(string username)
        {
            PublicProfileViewModel? viewModel = await this._profileService.GetPublicProfileAsync(username);
            return viewModel == null ? this.RedirectToAction("Index", "Home") : this.View(viewModel);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> BanUser(string username)
        {
            int adminId = this._userContextService.UserId ?? 0;
            if (adminId == 0)
            {
                return this.Forbid();
            }

            try
            {
                BanUserPageViewModel vm = await this._adminService.GetBanUserPageViewModelAsync(adminId, username);
                return this.View(vm);
            }
            catch (UnauthorizedAccessException)
            {
                return this.Forbid();
            }
            catch (KeyNotFoundException)
            {
                return this.NotFound();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ProcessBan(int userId, string username, BanViewModel banViewModel)
        {
            int adminId = this._userContextService.UserId ?? 0;
            if (adminId == 0)
            {
                return this.Forbid();
            }

            try
            {
                bool success = await this._adminService.BanUserAsync(adminId, username, banViewModel);
                if (success)
                {
                    this.TempData["SuccessMessage"] = banViewModel.BanType == BanType.Account
                        ? $"User {username} has been banned successfully." // using username
                        : "IP address ban applied successfully.";
                }
                else
                {
                    this.TempData["ErrorMessage"] = "Failed to ban user. Please try again.";
                }
                return this.RedirectToAction("PublicProfile", new { username });
            }
            catch (UnauthorizedAccessException)
            {
                return this.Forbid();
            }
            catch (KeyNotFoundException)
            {
                return this.NotFound();
            }
            catch (Exception ex)
            {
                this._logManager.Error("Unexpected error during ban process.", exception: ex);
                this.TempData["ErrorMessage"] = "An unexpected error occurred. Please try again later.";
                return this.RedirectToAction("PublicProfile", new { username });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> UnbanUser(int banId, string username)
        {
            int adminId = this._userContextService.UserId ?? 0;
            if (adminId == 0)
            {
                return this.Forbid();
            }

            try
            {
                bool success = await this._adminService.RemoveBanAsync(banId);
                if (success)
                {
                    this.TempData["SuccessMessage"] = "Ban has been removed successfully.";
                }
                else
                {
                    this.TempData["ErrorMessage"] = "Failed to remove ban. Please try again.";
                }
                return this.RedirectToAction("PublicProfile", new { username });
            }
            catch (UnauthorizedAccessException)
            {
                return this.Forbid();
            }
            catch (Exception ex)
            {
                this._logManager.Error("Unexpected error during unban process.", exception: ex);
                this.TempData["ErrorMessage"] = "An unexpected error occurred. Please try again later.";
                return this.RedirectToAction("PublicProfile", new { username });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ChangeUsername(ChangeUsernameViewModel changeUsernameViewModel)
        {
            if (!this.ModelState.IsValid)
            {
                int currentUserId = this._userContextService.UserId ?? 0;
                PrivateProfileViewModel? profileVm = await this._profileService.GetPrivateProfileAsync(currentUserId);
                profileVm.ChangeUsernameViewModel = changeUsernameViewModel;
                return this.View("PrivateProfile", profileVm);
            }

            int userId = this._userContextService.UserId ?? 0;
            if (userId == 0)
            {
                return this.RedirectToAction("Login");
            }

            try
            {
                // Delegate change to service
                await this._accountService.ChangeUsernameAsync(userId, changeUsernameViewModel.NewUsername);

                // Reissue authentication cookie with new username
                string tierValue = this.User.FindFirst("UserTier")?.Value ?? "0";
                List<Claim> claims = new()
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Name, changeUsernameViewModel.NewUsername),
                    new Claim("UserTier", tierValue)
                };
                this.AddAdminRoleClaim(claims, (UserTier)int.Parse(tierValue));

                ClaimsIdentity identity = new(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                ClaimsPrincipal principal = new(identity);
                await this.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                await this.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                });

                this.TempData["SuccessMessage"] = "Username changed successfully";
                return this.RedirectToAction("PrivateProfile");
            }
            catch (UserFriendlyExceptionBase ex)
            {
                this.ModelState.AddModelError("ChangeUsernameViewModel.NewUsername", ex.Message);
                PrivateProfileViewModel? profileOnError = await this._profileService.GetPrivateProfileAsync(userId);
                profileOnError.ChangeUsernameViewModel = changeUsernameViewModel;
                return this.View("PrivateProfile", profileOnError);
            }
            catch (Exception ex)
            {
                this._logManager.Error("Unexpected error during username change.", exception: ex);
                this.ModelState.AddModelError("", "An unexpected error occurred. Please try again later.");
                PrivateProfileViewModel? profileOnError = await this._profileService.GetPrivateProfileAsync(userId);
                profileOnError.ChangeUsernameViewModel = changeUsernameViewModel;
                return this.View("PrivateProfile", profileOnError);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel changePasswordViewModel)
        {
            if (!this.ModelState.IsValid)
            {
                int currentUserId = this._userContextService.UserId ?? 0;
                PrivateProfileViewModel? profileVm = await this._profileService.GetPrivateProfileAsync(currentUserId);
                profileVm.ChangePasswordViewModel = changePasswordViewModel;
                return this.View("PrivateProfile", profileVm);
            }

            int userId = this._userContextService?.UserId ?? 0;
            if (userId == 0)
            {
                return this.RedirectToAction("Login");
            }

            try
            {
                // Delegate change to service
                await this._accountService.ChangePasswordAsync(userId, changePasswordViewModel.CurrentPassword, changePasswordViewModel.NewPassword);

                this.TempData["SuccessMessage"] = "Password changed successfully";
                return this.RedirectToAction("PrivateProfile");
            }
            catch (UserFriendlyExceptionBase ex)
            {
                this.ModelState.AddModelError("CurrentPassword", ex.Message);
                return this.View("PrivateProfile", await this._profileService.GetPrivateProfileAsync(userId));
            }
            catch (Exception ex)
            {
                this._logManager.Error("Unexpected error during password change.", exception: ex);
                this.ModelState.AddModelError("", "An unexpected error occurred. Please try again later.");
                return this.View("PrivateProfile", await this._profileService.GetPrivateProfileAsync(userId));
            }
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return this.View();
        }

        private void AddAdminRoleClaim(List<Claim> claims, UserTier tier)
        {
            if (tier == UserTier.Admin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }
        }
    }
}