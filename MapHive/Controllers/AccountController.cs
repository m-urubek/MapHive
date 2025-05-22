namespace MapHive.Controllers;

using System.Security.Claims;
using MapHive.Filters;
using MapHive.Models.Enums;
using MapHive.Models.Exceptions;
using MapHive.Models.PageModels;
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
    IAccountService _accountService,
    IProfileService _profileService,
    ILogManagerService _logManagerService,
    IRecaptchaService _recaptchaService,
    IUserContextService _userContextService,
    IConfigurationSingleton _configurationSingleton,
    IOptions<RecaptchaSettings> _recaptchaOptions) : Controller
{

    [HttpGet("Login")]
    [OnlyAnonymous]
    public IActionResult Login()
    {
        return View(new LoginPageModel()
        {
            Username = "",
            Password = "",
        });
    }

    [HttpPost("Login")]
    [ValidateAntiForgeryToken]
    [OnlyAnonymous]
    public async Task<IActionResult> Login(LoginPageModel loginRequest)
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

    [HttpGet("Register")]
    [OnlyAnonymous]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost("Register")]
    [ValidateAntiForgeryToken]
    [OnlyAnonymous]
    public async Task<IActionResult> Register(RegisterPageModel registerRequest)
    {
        RecaptchaSettings recaptchaSettings = _recaptchaOptions.Value;
        bool devMode = await _configurationSingleton.DevelopmentModeAsync();
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

    [HttpPost("Logout")]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        if (!_userContextService.IsAuthenticated)
            throw new PublicErrorException(message: "User is not authenticated.");
        await _accountService.LogoutAsync();
        return RedirectToAction(actionName: "Index", controllerName: "Home");
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetDarkModePreference()
    {
        bool enabled = await _accountService.GetDarkModePreferenceAsync();
        return Json(new { enabled });
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> SetDarkModePreference(bool enabled)
    {
        await _accountService.SetDarkModePreferenceAsync(enabled);
        return Ok();
    }

    [HttpGet("MyProfile")]
    [Authorize]
    public async Task<IActionResult> PrivateProfile()
    {
        PrivateProfilePageModel? pageModel = await _profileService.GetPrivateProfilePageModelAsync();
        return pageModel == null ? RedirectToAction(actionName: "Login") : View(model: pageModel);
    }

    [HttpGet("Username/{username:required}")]
    public async Task<IActionResult> PublicProfileByName(string username)
    {
        PublicProfilePageModel pageModel = await _profileService.GetPublicProfileAsync(username: username);
        return View(viewName: "PublicProfile", model: pageModel);
    }

    [HttpGet("Profile/{id:int:required}")]
    public async Task<IActionResult> PublicProfileById(int id)
    {
        PublicProfilePageModel pageModel = await _profileService.GetPublicProfileAsync(accountId: id);
        return View(viewName: "PublicProfile", model: pageModel);
    }

    [HttpPost("ChangeUsername")]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> ChangeUsername(ChangeUsernamePageModel changeUsernamePageModel)
    {
        if (!ModelState.IsValid)
            return PartialView(changeUsernamePageModel);

        try
        {
            // Delegate change to service
            await _accountService.ChangeUsernameAsync(
                newUsername: changeUsernamePageModel.NewUsername!);

            // Reissue authentication cookie with new username
            string tierValue = User.FindFirst(type: "AccountTier")?.Value ?? "0";
            List<Claim> claims = new()
            {
                new Claim(type: ClaimTypes.NameIdentifier, value: _userContextService.AccountIdOrThrow.ToString()),
                new Claim(type: ClaimTypes.Name, value: changeUsernamePageModel.NewUsername!),
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
            ModelState.AddModelError(key: "ChangeUsernamePageModel.NewUsername", errorMessage: ex.Message);
            PrivateProfilePageModel? profileOnError = await _profileService.GetPrivateProfilePageModelAsync();
            profileOnError.ChangeUsernamePageModel = changeUsernamePageModel;
            return View(viewName: "PrivateProfile", model: profileOnError);
        }
    }

    [HttpPost("ChangePassword")]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordPageModel changePasswordPageModel)
    {
        if (!_userContextService.IsAuthenticated)
            throw new PublicErrorException(message: "User is not authenticated.");

        if (!ModelState.IsValid)
        {
            return PartialView(changePasswordPageModel);
        }

        try
        {
            // Delegate change to service
            await _accountService.ChangePasswordAsync(
                currentPassword: changePasswordPageModel.CurrentPassword!,
                newPassword: changePasswordPageModel.NewPassword!);

            TempData["SuccessMessage"] = "Password changed successfully";
            return RedirectToAction(actionName: "PrivateProfile");
        }
        catch (UserFriendlyExceptionBase ex)
        {
            ModelState.AddModelError(key: "CurrentPassword", errorMessage: ex.Message);
            return View(viewName: "PrivateProfile", model: await _profileService.GetPrivateProfilePageModelAsync());
        }
    }

    #region Bans

    [HttpGet("BanUser/{id:int:required}")]
    [Authorize(Roles = "Admin,2")]
    public async Task<IActionResult> BanUser(int id)
    {
        return View(model: await _accountService.GetBanUserPagePageModelAsync(accountId: id));
    }

    [HttpPost("UnbanUser/{id:int:required}")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,2")]
    public async Task<IActionResult> UnbanUser(int id)
    {
        await _accountService.UnbanUserAsync(accountId: id);
        return RedirectToAction(actionName: "PublicProfileById", routeValues: new { id });
    }

    [HttpPost("Ban/{id:int:required}")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,2")]
    public async Task<IActionResult> Ban(int id, BanUserUpdatePageModel banPageModel)
    {
        if (!ModelState.IsValid)
        {
            return View(model: banPageModel);
        }
        _ = await _accountService.BanAsync(accountId: id, banPageModel: banPageModel);
        return RedirectToAction(actionName: "PublicProfileById", controllerName: "Account", routeValues: new { id });
    }

    #endregion

    [HttpGet("AccessDenied")]
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
