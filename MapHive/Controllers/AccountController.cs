using MapHive.Models;
using MapHive.Models.Exceptions;
using MapHive.Singletons;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using reCAPTCHA.AspNetCore;
using System.Security.Claims;

namespace MapHive.Controllers
{
    public class AccountController : Controller
    {
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

            AuthResponse response = await CurrentRequest.AuthService.LoginAsync(model);

            if (response.Success && response.User != null)
            {
                // Create claims
                List<Claim> claims = new()
                {
                    new Claim(ClaimTypes.Name, response.User.Username),
                    new Claim(ClaimTypes.NameIdentifier, response.User.Id.ToString()),
                    new Claim("UserTier", ((int)response.User.Tier).ToString()),
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
            // Add explicit check for null or empty RecaptchaResponse and try to get it from form data
            if (string.IsNullOrEmpty(model.RecaptchaResponse))
            {
                // Fallback: try to get the reCAPTCHA response directly from the form data
                string recaptchaResponse = this.Request.Form["g-recaptcha-response"].ToString();
                if (!string.IsNullOrEmpty(recaptchaResponse))
                {
                    // If found in form data, use it
                    model.RecaptchaResponse = recaptchaResponse;

                    // Remove the error state for RecaptchaResponse if it exists
                    if (this.ModelState.ContainsKey("RecaptchaResponse"))
                    {
                        _ = this.ModelState.Remove("RecaptchaResponse");
                    }
                }
            }

            if (!this.ModelState.IsValid)
            {
                return this.View(model);
            }

            // Get reCAPTCHA settings from configuration (for test key detection)
            RecaptchaSettings recaptchaSettings = CurrentRequest.RecaptchaService.RecaptchaSettings;
            bool isUsingTestKeys = MainClient.AppSettings.DevelopmentMode && recaptchaSettings.SiteKey == "6LeIxAcTAAAAAJcZVRqyHh71UMIEGNQ_MXjiZKhI";

            // For test keys, accept any non-null response
            if (isUsingTestKeys && !string.IsNullOrEmpty(model.RecaptchaResponse))
            {
                // Using test keys with a response, so we'll consider it valid
                CurrentRequest.LogManager.Information("Accepting reCAPTCHA test response");
            }
            // Skip server-side validation when using test keys
            else if (!isUsingTestKeys)
            {
                // Only validate if not using test keys
                RecaptchaResponse validationResponse = await CurrentRequest.RecaptchaService.Validate(model.RecaptchaResponse);
                if (!validationResponse.success)
                {
                    // Log the failure
                    CurrentRequest.LogManager.Warning($"reCAPTCHA validation failed for user registration attempt");

                    // Add error to model state
                    this.ModelState.AddModelError("RecaptchaResponse", "reCAPTCHA verification failed. Please try again.");
                    return this.View(model);
                }
            }

            // Get client IP address
            string? ipAddress = CurrentRequest.HttpContext.HttpContext?.Connection.RemoteIpAddress?.ToString();
            if (string.IsNullOrEmpty(ipAddress))
            {
                throw new RedUserException("Unable to retreive your IP address");
            }

            // Create the user
            AuthResponse response = await CurrentRequest.AuthService.RegisterAsync(model, ipAddress);

            if (response.Success && response.User != null)
            {
                // Create claims
                List<Claim> claims = new()
                {
                    new Claim(ClaimTypes.Name, response.User.Username),
                    new Claim(ClaimTypes.NameIdentifier, response.User.Id.ToString()),
                    new Claim("UserTier", ((int)response.User.Tier).ToString()),
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
            // Redirect to the private profile when user accesses /Account/Profile
            return RedirectToAction("PrivateProfile");
        }
        
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> PrivateProfile()
        {
            string? userId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null || !int.TryParse(userId, out int id))
            {
                this.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).GetAwaiter().GetResult();
                throw new OrangeUserException("Your session is invalid, login again.");
            }

            User? user = CurrentRequest.UserRepository.GetUserById(id);
            if (user == null)
            {
                this.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).GetAwaiter().GetResult();
                throw new OrangeUserException("Your session is invalid, login again.");
            }

            // Get the user's places
            IEnumerable<MapLocation> userLocations = await CurrentRequest.MapRepository.GetLocationsByUserIdAsync(id);

            // Get the user's threads
            IEnumerable<DiscussionThread> userThreads = await CurrentRequest.DiscussionRepository.GetThreadsByUserIdAsync(id);

            PrivateProfileViewModel model = new()
            {
                Username = user.Username,
                Tier = user.Tier,
                RegistrationDate = user.RegistrationDate,
                UserLocations = userLocations,
                UserThreads = userThreads
            };

            return this.View(model);
        }
        
        [HttpGet]
        public async Task<IActionResult> PublicProfile(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Index", "Home");
            }

            // Check if user exists
            User? user = CurrentRequest.UserRepository.GetUserByUsername(username);
            if (user == null)
            {
                return NotFound();
            }

            // If logged-in user is viewing their own profile, redirect to private profile
            string? currentUserId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId != null && int.TryParse(currentUserId, out int id) && user.Id == id)
            {
                return RedirectToAction("PrivateProfile");
            }

            // Get the user's places
            IEnumerable<MapLocation> userLocations = await CurrentRequest.MapRepository.GetLocationsByUserIdAsync(user.Id);

            // Get the user's threads
            IEnumerable<DiscussionThread> userThreads = await CurrentRequest.DiscussionRepository.GetThreadsByUserIdAsync(user.Id);

            PublicProfileViewModel model = new()
            {
                Username = user.Username,
                Tier = user.Tier,
                RegistrationDate = user.RegistrationDate,
                UserLocations = userLocations,
                UserThreads = userThreads
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
                PrivateProfileViewModel? profileModel = this.GetCurrentUserPrivateProfile();
                return profileModel == null ? this.RedirectToAction("Login") : this.View("PrivateProfile", profileModel);
            }

            string? userId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null || !int.TryParse(userId, out int id))
            {
                return this.RedirectToAction("Login");
            }

            User? user = CurrentRequest.UserRepository.GetUserById(id);
            if (user == null)
            {
                return this.RedirectToAction("Login");
            }

            // Check if the username already exists
            if (CurrentRequest.UserRepository.CheckUsernameExists(model.NewUsername) && !user.Username.Equals(model.NewUsername, StringComparison.OrdinalIgnoreCase))
            {
                this.ModelState.AddModelError("ChangeUsernameModel.NewUsername", "Username already exists");
                PrivateProfileViewModel? profileModel = this.GetCurrentUserPrivateProfile();
                return profileModel == null ? this.RedirectToAction("Login") : this.View("PrivateProfile", profileModel);
            }

            // Update the username
            user.Username = model.NewUsername;
            CurrentRequest.UserRepository.UpdateUser(user);

            // Update user claims
            List<Claim> claims = new()
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("UserTier", ((int)user.Tier).ToString()),
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
            return this.RedirectToAction("PrivateProfile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if (!this.ModelState.IsValid)
            {
                PrivateProfileViewModel? profileModel = this.GetCurrentUserPrivateProfile();
                return profileModel == null ? this.RedirectToAction("Login") : this.View("PrivateProfile", profileModel);
            }

            string? userId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null || !int.TryParse(userId, out int id))
            {
                return this.RedirectToAction("Login");
            }

            User? user = CurrentRequest.UserRepository.GetUserById(id);
            if (user == null)
            {
                return this.RedirectToAction("Login");
            }

            // Verify current password
            if (!CurrentRequest.AuthService.VerifyPassword(model.CurrentPassword, user.PasswordHash))
            {
                this.ModelState.AddModelError("ChangePasswordModel.CurrentPassword", "Current password is incorrect");
                PrivateProfileViewModel? profileModel = this.GetCurrentUserPrivateProfile();
                return profileModel == null ? this.RedirectToAction("Login") : this.View("PrivateProfile", profileModel);
            }

            // Update password
            user.PasswordHash = CurrentRequest.AuthService.HashPassword(model.NewPassword);
            CurrentRequest.UserRepository.UpdateUser(user);

            this.TempData["SuccessMessage"] = "Password changed successfully";
            return this.RedirectToAction("PrivateProfile");
        }

        private PrivateProfileViewModel? GetCurrentUserPrivateProfile()
        {
            string? userId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null || !int.TryParse(userId, out int id))
            {
                return null;
            }

            User? user = CurrentRequest.UserRepository.GetUserById(id);
            if (user == null)
            {
                return null;
            }

            // Get user locations
            IEnumerable<MapLocation> userLocations = CurrentRequest.MapRepository.GetLocationsByUserIdAsync(id).GetAwaiter().GetResult();

            // Get user threads
            IEnumerable<DiscussionThread> userThreads = CurrentRequest.DiscussionRepository.GetThreadsByUserIdAsync(id).GetAwaiter().GetResult();

            return new PrivateProfileViewModel
            {
                Username = user.Username,
                Tier = user.Tier,
                RegistrationDate = user.RegistrationDate,
                UserLocations = userLocations,
                UserThreads = userThreads
            };
        }
    }
}