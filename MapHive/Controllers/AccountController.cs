namespace MapHive.Controllers
{
    using System.Security.Claims;
    using MapHive.Filters;
    using MapHive.Models;
    using MapHive.Models.Enums;
    using MapHive.Models.Exceptions;
    using MapHive.Models.ViewModels;
    using MapHive.Services;
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
        IConfigurationService configSingleton,
        IUserContextService userContextService,
        IOptions<RecaptchaSettings> recaptchaOptions) : Controller
    {
        private readonly IAccountService _accountService = accountService;
        private readonly IAdminService _adminService = adminService;
        private readonly IProfileService _profileService = profileService;
        private readonly ILogManagerService _logManagerService = logManager;
        private readonly IRecaptchaService _recaptchaService = recaptchaService;
        private readonly RecaptchaSettings _recaptchaSettings = recaptchaOptions.Value;
        private readonly IConfigurationService _configSingleton = configSingleton;
        private readonly IUserContextService _userContextService = userContextService;

        [HttpGet]
        [OnlyAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [OnlyAnonymous]
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
        }

        [HttpGet]
        [OnlyAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [OnlyAnonymous]
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
                    throw new PublicWarningException(message: "Unable to retreive reCAPTCHA response!");
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

                if (validationResponse?.success != true)
                {
                    _ = _logManagerService.LogAsync(severity: LogSeverity.Warning, message: "reCAPTCHA validation failed for user registration attempt.");
                    ModelState.AddModelError(key: "RecaptchaResponse", errorMessage: "reCAPTCHA verification failed. Please try again.");
                    return View(model: registerRequest);
                }
            }
            else if (!string.IsNullOrEmpty(value: registerRequest.RecaptchaResponse))
            {
                _ = _logManagerService.LogAsync(severity: LogSeverity.Information, message: "Accepting reCAPTCHA test response during registration.");
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
                _ = _logManagerService.LogAsync(severity: LogSeverity.Error, message: $"Unexpected error during registration for {registerRequest.Username}.", exception: ex);
                ModelState.AddModelError(key: "", errorMessage: "An unexpected error occurred during registration. Please try again later.");
                return View(model: registerRequest);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            if (!_userContextService.IsAuthenticated)
                throw new PublicErrorException(message: "User is not authenticated.");
            await _accountService.LogoutAsync();
            return RedirectToAction(actionName: "Index", controllerName: "Home");
        }

        [HttpGet("MyProfile")]
        [Authorize]
        public async Task<IActionResult> PrivateProfile()
        {
            PrivateProfileViewModel? viewModel = await _profileService.GetPrivateProfileAsync();
            return viewModel == null ? RedirectToAction(actionName: "Login") : View(model: viewModel);
        }

        [HttpGet("Profile/{username}")]
        public async Task<IActionResult> PublicProfileByName(string username)
        {
            PublicProfileViewModel viewModel = await _profileService.GetPublicProfileAsync(username: username);
            return View(viewName: "PublicProfile", model: viewModel);
        }

        [HttpGet("Profile/Id/{accountId:int}")]
        public async Task<IActionResult> PublicProfileById(int accountId)
        {
            PublicProfileViewModel viewModel = await _profileService.GetPublicProfileAsync(accountId: accountId);
            return View(viewName: "PublicProfile", model: viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ChangeUsername(ChangeUsernameViewModel changeUsernameViewModel)
        {
            if (!ModelState.IsValid)
            {
                PrivateProfileViewModel? profileVm = await _profileService.GetPrivateProfileAsync();
                profileVm.ChangeUsernameViewModel = changeUsernameViewModel;
                return View(viewName: "PrivateProfile", model: profileVm);
            }

            int accountId = _userContextService.AccountIdRequired;
            if (accountId == 0)
            {
                return RedirectToAction(actionName: "Login");
            }

            try
            {
                // Delegate change to service
                await _accountService.ChangeUsernameAsync(accountId: accountId, newUsername: changeUsernameViewModel.NewUsername);

                // Reissue authentication cookie with new username
                string tierValue = User.FindFirst(type: "AccountTier")?.Value ?? "0";
                List<Claim> claims = new()
                {
                    new Claim(type: ClaimTypes.NameIdentifier, value: accountId.ToString()),
                    new Claim(type: ClaimTypes.Name, value: changeUsernameViewModel.NewUsername),
                    new Claim(type: "AccountTier", value: tierValue)
                };
                AddAdminRoleClaim(claims: claims, tier: (AccountTier)int.Parse(s: tierValue));

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
                PrivateProfileViewModel? profileOnError = await _profileService.GetPrivateProfileAsync();
                profileOnError.ChangeUsernameViewModel = changeUsernameViewModel;
                return View(viewName: "PrivateProfile", model: profileOnError);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel changePasswordViewModel)
        {
            if (!_userContextService.IsAuthenticated)
                throw new PublicErrorException(message: "User is not authenticated.");

            if (!ModelState.IsValid)
            {
                PrivateProfileViewModel? profileVm = await _profileService.GetPrivateProfileAsync();
                profileVm.ChangePasswordViewModel = changePasswordViewModel;
                return View(viewName: "PrivateProfile", model: profileVm);
            }

            int accountId = _userContextService.AccountIdRequired;
            if (accountId == 0)
            {
                return RedirectToAction(actionName: "Login");
            }

            try
            {
                // Delegate change to service
                await _accountService.ChangePasswordAsync(accountId: accountId, currentPassword: changePasswordViewModel.CurrentPassword, newPassword: changePasswordViewModel.NewPassword);

                TempData["SuccessMessage"] = "Password changed successfully";
                return RedirectToAction(actionName: "PrivateProfile");
            }
            catch (UserFriendlyExceptionBase ex)
            {
                ModelState.AddModelError(key: "CurrentPassword", errorMessage: ex.Message);
                return View(viewName: "PrivateProfile", model: await _profileService.GetPrivateProfileAsync());
            }
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private static void AddAdminRoleClaim(List<Claim> claims, AccountTier tier)
        {
            if (tier == AccountTier.Admin)
            {
                claims.Add(item: new Claim(type: ClaimTypes.Role, value: "Admin"));
            }
        }
    }
}
