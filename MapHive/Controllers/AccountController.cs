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

            // First authenticate the user to check if they exist and credentials are valid
            AuthResponse response = await CurrentRequest.AuthService.LoginAsync(model);

            if (response.Success && response.User != null)
            {
                // Get client IP address
                string? ipAddress = CurrentRequest.HttpContext.HttpContext?.Connection.RemoteIpAddress?.ToString();

                // Check if the user is banned (account ban)
                UserBan? userBan = await CurrentRequest.UserRepository.GetActiveBanByUserIdAsync(response.User.Id);
                if (userBan != null && userBan.IsActive)
                {
                    string banMessage = userBan.ExpiresAt.HasValue
                        ? $"Your account has been banned until {userBan.ExpiresAt.Value.ToString("g")}. Reason: {userBan.Reason}"
                        : $"Your account has been permanently banned. Reason: {userBan.Reason}";

                    this.ModelState.AddModelError("", banMessage);
                    return this.View(model);
                }

                // Check for IP ban only if user is not an admin
                if (!string.IsNullOrEmpty(ipAddress) && response.User.Tier != UserTier.Admin)
                {
                    UserBan? ipBan = await CurrentRequest.UserRepository.GetActiveBanByIpAddressAsync(ipAddress);
                    if (ipBan != null && ipBan.IsActive)
                    {
                        this.ModelState.AddModelError("", $"Your IP address has been banned. Reason: {ipBan.Reason}");
                        return this.View(model);
                    }
                }

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

            // Check if the IP is banned for registration
            // Note: All new registrations are subject to IP bans since new users always start as normal users
            UserBan? ipBan = await CurrentRequest.UserRepository.GetActiveBanByIpAddressAsync(ipAddress);
            if (ipBan != null && ipBan.IsActive)
            {
                this.ModelState.AddModelError("", $"Registration from your IP address is not allowed. Reason: {ipBan.Reason}");
                return this.View(model);
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
        public IActionResult Profile()
        {
            // Redirect to the private profile when user accesses /Account/Profile
            return this.RedirectToAction("PrivateProfile");
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
                return this.RedirectToAction("Index", "Home");
            }

            // Check if user exists
            User? user = CurrentRequest.UserRepository.GetUserByUsername(username);
            if (user == null)
            {
                return this.NotFound();
            }

            // Only redirect to private profile if the user is viewing their own profile
            // AND they didn't explicitly request to see their public profile
            string? currentUserId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            bool isOwnProfile = currentUserId != null && int.TryParse(currentUserId, out int id) && user.Id == id;

            // Check if the request came from the "Show Public Profile" button
            bool isExplicitPublicProfileRequest = this.Request.Headers["Referer"].ToString().Contains("/PrivateProfile");

            // Only redirect if viewing own profile and not explicitly requesting public view
            if (isOwnProfile && !isExplicitPublicProfileRequest && !this.Request.Query.ContainsKey("viewPublic"))
            {
                return this.RedirectToAction("PrivateProfile");
            }

            // Get the user's places
            IEnumerable<MapLocation> userLocations = await CurrentRequest.MapRepository.GetLocationsByUserIdAsync(user.Id);

            // Get the user's threads
            IEnumerable<DiscussionThread> userThreads = await CurrentRequest.DiscussionRepository.GetThreadsByUserIdAsync(user.Id);

            // Check if current user is an admin
            bool isAdmin = false;
            if (currentUserId != null && int.TryParse(currentUserId, out int currentId))
            {
                User? currentUser = CurrentRequest.UserRepository.GetUserById(currentId);
                isAdmin = currentUser != null && currentUser.Tier == UserTier.Admin;
            }

            // Get active ban if any
            UserBan? activeBan = await CurrentRequest.UserRepository.GetActiveBanByUserIdAsync(user.Id);

            PublicProfileViewModel model = new()
            {
                UserId = user.Id,
                Username = user.Username,
                Tier = user.Tier,
                RegistrationDate = user.RegistrationDate,
                IpAddress = user.IpAddress, // Only visible to admins in the view
                UserLocations = userLocations,
                UserThreads = userThreads,
                IsAdmin = isAdmin,
                CurrentBan = activeBan
            };

            return this.View(model);
        }

        [HttpGet]
        [Authorize]
        public IActionResult BanUser(string username)
        {
            // Check if current user is an admin
            string? currentUserId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId) || !int.TryParse(currentUserId, out int adminId))
            {
                return this.Forbid();
            }

            User? admin = CurrentRequest.UserRepository.GetUserById(adminId);
            if (admin == null || admin.Tier != UserTier.Admin)
            {
                return this.Forbid();
            }

            // Check if target user exists
            User? user = CurrentRequest.UserRepository.GetUserByUsername(username);
            if (user == null)
            {
                return this.NotFound();
            }

            // Set up viewbag data
            this.ViewBag.Username = user.Username;
            this.ViewBag.UserId = user.Id;
            this.ViewBag.IpAddress = user.IpAddress;
            this.ViewBag.UserTier = user.Tier;

            return this.View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ProcessBan(int userId, string username, BanViewModel model)
        {
            // Check if current user is an admin
            string? currentUserId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId) || !int.TryParse(currentUserId, out int adminId))
            {
                return this.Forbid();
            }

            User? admin = CurrentRequest.UserRepository.GetUserById(adminId);
            if (admin == null || admin.Tier != UserTier.Admin)
            {
                return this.Forbid();
            }

            // Check if the target user exists
            User? user = CurrentRequest.UserRepository.GetUserById(userId);
            if (user == null)
            {
                return this.NotFound();
            }

            // Prevent admins from banning other admins
            if (user.Tier == UserTier.Admin)
            {
                this.TempData["ErrorMessage"] = "Cannot ban another administrator.";
                return this.RedirectToAction("PublicProfile", new { username });
            }

            if (!this.ModelState.IsValid)
            {
                this.TempData["ErrorMessage"] = "Invalid ban parameters.";
                return this.RedirectToAction("BanUser", new { username });
            }

            // Create the ban object
            UserBan ban = new()
            {
                BannedByUserId = adminId,
                Reason = model.Reason,
                BanType = model.BanType,
                BannedAt = DateTime.UtcNow
            };

            // Set expiry date if not permanent
            if (!model.IsPermanent && model.BanDurationDays.HasValue)
            {
                ban.ExpiresAt = DateTime.UtcNow.AddDays(model.BanDurationDays.Value);
            }

            // Set the appropriate ID based on ban type
            if (model.BanType == BanType.Account)
            {
                ban.UserId = userId;
            }
            else if (model.BanType == BanType.IpAddress)
            {
                ban.IpAddress = user.IpAddress;
            }

            // Save the ban
            int banId = await CurrentRequest.UserRepository.BanUserAsync(ban);

            if (banId > 0)
            {
                this.TempData["SuccessMessage"] = model.BanType == BanType.Account
                    ? $"User {user.Username} has been banned successfully."
                    : $"IP address {user.IpAddress} has been banned successfully.";
            }
            else
            {
                this.TempData["ErrorMessage"] = "Failed to ban user. Please try again.";
            }

            return this.RedirectToAction("PublicProfile", new { username });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> UnbanUser(int banId, string username)
        {
            // Check if current user is an admin
            string? currentUserId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId) || !int.TryParse(currentUserId, out int adminId))
            {
                return this.Forbid();
            }

            User? admin = CurrentRequest.UserRepository.GetUserById(adminId);
            if (admin == null || admin.Tier != UserTier.Admin)
            {
                return this.Forbid();
            }

            // Remove the ban
            bool success = await CurrentRequest.UserRepository.UnbanUserAsync(banId);

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