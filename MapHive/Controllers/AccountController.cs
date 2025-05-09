namespace MapHive.Controllers
{
    using System.Security.Claims;
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
    using Microsoft.Extensions.Primitives;
    using reCAPTCHA.AspNetCore;

    public class AccountController(
        IAccountService accountService,
        IAdminService adminService,
        IProfileService profileService,
        ILogManagerService logManager,
        IRecaptchaService recaptchaService,
        IConfigurationSingleton configSingleton,
        IUserContextService userContextService,
        IOptions<RecaptchaSettings> recaptchaOptions) : Controller
    {
        private readonly IAccountService _accountService = accountService;
        private readonly IAdminService _adminService = adminService;
        private readonly IProfileService _profileService = profileService;
        private readonly ILogManagerService _logManagerService = logManager;
        private readonly IRecaptchaService _recaptchaService = recaptchaService;
        private readonly RecaptchaSettings _recaptchaSettings = recaptchaOptions.Value;
        private readonly IConfigurationSingleton _configSingleton = configSingleton;
        private readonly IUserContextService _userContextService = userContextService;

        [HttpGet]
        public IActionResult Login()
        {
            return _userContextService.IsAuthenticated ? RedirectToAction(actionName: "Index", controllerName: "Home") : View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginRequest loginRequest)
        {
            if (!ModelState.IsValid)
            {
                return View(model: loginRequest);
            }

            try
            {
                _ = await _accountService.LoginAsync(request: loginRequest);
                return RedirectToAction(actionName: "Index", controllerName: "Home");
            }
            catch (UserFriendlyExceptionBase ex)
            {
                ModelState.AddModelError(key: "", errorMessage: ex.Message);
                return View(model: loginRequest);
            }
            catch (Exception ex)
            {
                _logManagerService.Log(severity: LogSeverity.Error, message: $"Unexpected error during login for {loginRequest.Username}.", exception: ex);
                ModelState.AddModelError(key: "", errorMessage: "An unexpected error occurred during login. Please try again later.");
                return View(model: loginRequest);
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            return _userContextService.IsAuthenticated ? RedirectToAction(actionName: "Index", controllerName: "Home") : View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterRequest registerRequest)
        {
            RecaptchaSettings recaptchaSettings = _recaptchaSettings;
            bool devMode = await _configSingleton.GetDevelopmentModeAsync();
            bool isUsingTestKeys = devMode && recaptchaSettings.SiteKey == "6LeIxAcTAAAAAJcZVRqyHh71UMIEGNQ_MXjiZKhI";

            if (string.IsNullOrEmpty(value: registerRequest.RecaptchaResponse) && Request.Form.ContainsKey(key: "g-recaptcha-response"))
            {
                StringValues recaptchaResponse = Request.Form["g-recaptcha-response"];
                if (recaptchaResponse.Count == 0 || string.IsNullOrWhiteSpace(recaptchaResponse.ToString()))
                {
                    throw new WarningException(message: "Unable to retreive reCAPTCHA response!");
                }

                registerRequest.RecaptchaResponse = recaptchaResponse.ToString();
                _ = ModelState.Remove(key: "RecaptchaResponse");
            }

            if (!ModelState.IsValid)
            {
                return View(model: registerRequest);
            }

            if (!isUsingTestKeys)
            {
                RecaptchaResponse? validationResponse = null;
                if (!string.IsNullOrEmpty(value: registerRequest.RecaptchaResponse))
                {
                    validationResponse = await _recaptchaService.Validate(responseCode: registerRequest.RecaptchaResponse);
                }

                if (validationResponse == null || !validationResponse.success)
                {
                    _logManagerService.Log(severity: LogSeverity.Warning, message: "reCAPTCHA validation failed for user registration attempt.");
                    ModelState.AddModelError(key: "RecaptchaResponse", errorMessage: "reCAPTCHA verification failed. Please try again.");
                    return View(model: registerRequest);
                }
            }
            else if (!string.IsNullOrEmpty(value: registerRequest.RecaptchaResponse))
            {
                _logManagerService.Log(severity: LogSeverity.Information, message: "Accepting reCAPTCHA test response during registration.");
            }
            else
            {
                ModelState.AddModelError(key: "RecaptchaResponse", errorMessage: "reCAPTCHA response missing (Test Mode).");
                return View(model: registerRequest);
            }

            try
            {
                _ = await _accountService.RegisterAsync(request: registerRequest);
                return RedirectToAction(actionName: "Index", controllerName: "Home");
            }
            catch (UserFriendlyExceptionBase ex)
            {
                ModelState.AddModelError(key: "", errorMessage: ex.Message);
                return View(model: registerRequest);
            }
            catch (Exception ex)
            {
                _logManagerService.Log(severity: LogSeverity.Error, message: $"Unexpected error during registration for {registerRequest.Username}.", exception: ex);
                ModelState.AddModelError(key: "", errorMessage: "An unexpected error occurred during registration. Please try again later.");
                return View(model: registerRequest);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _accountService.LogoutAsync();
            return RedirectToAction(actionName: "Index", controllerName: "Home");
        }

        [HttpGet]
        [Authorize]
        public IActionResult Profile()
        {
            return RedirectToAction(actionName: "PrivateProfile");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> PrivateProfile()
        {
            int userId = _userContextService.UserId;
            PrivateProfileViewModel? viewModel = await _profileService.GetPrivateProfileAsync(userId: userId);
            return viewModel == null ? RedirectToAction(actionName: "Login") : View(model: viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> PublicProfile(string username)
        {
            PublicProfileViewModel? viewModel = await _profileService.GetPublicProfileAsync(username: username);
            return viewModel == null ? RedirectToAction(actionName: "Index", controllerName: "Home") : View(model: viewModel);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> BanUser(string username)
        {
            int adminId = _userContextService.UserId;
            if (adminId == 0)
            {
                return Forbid();
            }

            try
            {
                BanUserPageViewModel vm = await _adminService.GetBanUserPageViewModelAsync(adminId: adminId, username: username);
                return View(model: vm);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ProcessBan(string username, BanViewModel banViewModel)
        {
            int adminId = _userContextService.UserId;
            if (adminId == 0)
            {
                return Forbid();
            }

            try
            {
                bool success = await _adminService.BanUserAsync(adminId: adminId, username: username, model: banViewModel);
                if (success)
                {
                    TempData["SuccessMessage"] = banViewModel.BanType == BanType.Account
                        ? $"User {username} has been banned successfully." // using username
                        : "IP address ban applied successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to ban user. Please try again.";
                }
                return RedirectToAction(actionName: "PublicProfile", routeValues: new { username });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logManagerService.Log(severity: LogSeverity.Error, message: "Unexpected error during ban process.", exception: ex);
                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again later.";
                return RedirectToAction(actionName: "PublicProfile", routeValues: new { username });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> UnbanUser(int banId, string username)
        {
            int adminId = _userContextService.UserId;
            if (adminId == 0)
            {
                return Forbid();
            }

            try
            {
                bool success = await _adminService.RemoveBanAsync(id: banId);
                if (success)
                {
                    TempData["SuccessMessage"] = "Ban has been removed successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to remove ban. Please try again.";
                }
                return RedirectToAction(actionName: "PublicProfile", routeValues: new { username });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logManagerService.Log(severity: LogSeverity.Error, message: "Unexpected error during unban process.", exception: ex);
                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again later.";
                return RedirectToAction(actionName: "PublicProfile", routeValues: new { username });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ChangeUsername(ChangeUsernameViewModel changeUsernameViewModel)
        {
            if (!ModelState.IsValid)
            {
                int currentUserId = _userContextService.UserId;
                PrivateProfileViewModel? profileVm = await _profileService.GetPrivateProfileAsync(userId: currentUserId);
                profileVm.ChangeUsernameViewModel = changeUsernameViewModel;
                return View(viewName: "PrivateProfile", model: profileVm);
            }

            int userId = _userContextService.UserId;
            if (userId == 0)
            {
                return RedirectToAction(actionName: "Login");
            }

            try
            {
                // Delegate change to service
                await _accountService.ChangeUsernameAsync(userId: userId, newUsername: changeUsernameViewModel.NewUsername);

                // Reissue authentication cookie with new username
                string tierValue = User.FindFirst(type: "UserTier")?.Value ?? "0";
                List<Claim> claims = new()
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Name, changeUsernameViewModel.NewUsername),
                    new Claim("UserTier", tierValue)
                };
                AddAdminRoleClaim(claims: claims, tier: (UserTier)int.Parse(s: tierValue));

                ClaimsIdentity identity = new(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                ClaimsPrincipal principal = new(identity);
                await HttpContext.SignOutAsync(scheme: CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(scheme: CookieAuthenticationDefaults.AuthenticationScheme, principal: principal, properties: new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(days: 7)
                });

                TempData["SuccessMessage"] = "Username changed successfully";
                return RedirectToAction(actionName: "PrivateProfile");
            }
            catch (UserFriendlyExceptionBase ex)
            {
                ModelState.AddModelError(key: "ChangeUsernameViewModel.NewUsername", errorMessage: ex.Message);
                PrivateProfileViewModel? profileOnError = await _profileService.GetPrivateProfileAsync(userId: userId);
                profileOnError.ChangeUsernameViewModel = changeUsernameViewModel;
                return View(viewName: "PrivateProfile", model: profileOnError);
            }
            catch (Exception ex)
            {
                _logManagerService.Log(severity: LogSeverity.Error, message: "Unexpected error during username change.", exception: ex);
                ModelState.AddModelError(key: "", errorMessage: "An unexpected error occurred. Please try again later.");
                PrivateProfileViewModel? profileOnError = await _profileService.GetPrivateProfileAsync(userId: userId);
                profileOnError.ChangeUsernameViewModel = changeUsernameViewModel;
                return View(viewName: "PrivateProfile", model: profileOnError);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel changePasswordViewModel)
        {
            if (!ModelState.IsValid)
            {
                int currentUserId = _userContextService.UserId;
                PrivateProfileViewModel? profileVm = await _profileService.GetPrivateProfileAsync(userId: currentUserId);
                profileVm.ChangePasswordViewModel = changePasswordViewModel;
                return View(viewName: "PrivateProfile", model: profileVm);
            }

            int userId = _userContextService.UserId;
            if (userId == 0)
            {
                return RedirectToAction(actionName: "Login");
            }

            try
            {
                // Delegate change to service
                await _accountService.ChangePasswordAsync(userId: userId, currentPassword: changePasswordViewModel.CurrentPassword, newPassword: changePasswordViewModel.NewPassword);

                TempData["SuccessMessage"] = "Password changed successfully";
                return RedirectToAction(actionName: "PrivateProfile");
            }
            catch (UserFriendlyExceptionBase ex)
            {
                ModelState.AddModelError(key: "CurrentPassword", errorMessage: ex.Message);
                return View(viewName: "PrivateProfile", model: await _profileService.GetPrivateProfileAsync(userId: userId));
            }
            catch (Exception ex)
            {
                _logManagerService.Log(severity: LogSeverity.Error, message: "Unexpected error during password change.", exception: ex);
                ModelState.AddModelError(key: "", errorMessage: "An unexpected error occurred. Please try again later.");
                return View(viewName: "PrivateProfile", model: await _profileService.GetPrivateProfileAsync(userId: userId));
            }
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private static void AddAdminRoleClaim(List<Claim> claims, UserTier tier)
        {
            if (tier == UserTier.Admin)
            {
                claims.Add(item: new Claim(ClaimTypes.Role, "Admin"));
            }
        }
    }
}
