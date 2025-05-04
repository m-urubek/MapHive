using AutoMapper;
using MapHive.Models;
using MapHive.Models.BusinessModels;
using MapHive.Models.Enums;
using MapHive.Models.Exceptions;
using MapHive.Models.RepositoryModels;
using MapHive.Models.ViewModels;
using MapHive.Repositories;
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
        private readonly IAuthService _authService;
        private readonly IUserRepository _userRepository;
        private readonly ILogManagerSingleton _logManager;
        private readonly IRecaptchaService _recaptchaService;
        private readonly RecaptchaSettings _recaptchaSettings;
        private readonly IConfigurationSingleton _configSingleton;
        private readonly IUserContextService _userContextService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMapLocationRepository _mapLocation;
        private readonly IDiscussionRepository _discussionRepository;
        private readonly IMapper _mapper;

        public AccountController(
            IAuthService authService,
            IUserRepository userRepository,
            ILogManagerSingleton logManager,
            IRecaptchaService recaptchaService,
            IConfigurationSingleton configSingleton,
            IUserContextService userContextService,
            IHttpContextAccessor httpContextAccessor,
            IOptions<RecaptchaSettings> recaptchaOptions,
            IMapLocationRepository mapLocation,
            IDiscussionRepository discussionRepository,
            IMapper mapper)
        {
            this._authService = authService;
            this._userRepository = userRepository;
            this._logManager = logManager;
            this._recaptchaService = recaptchaService;
            this._recaptchaSettings = recaptchaOptions.Value;
            this._configSingleton = configSingleton;
            this._userContextService = userContextService;
            this._httpContextAccessor = httpContextAccessor;
            this._mapLocation = mapLocation;
            this._discussionRepository = discussionRepository;
            this._mapper = mapper;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return this._userContextService.IsAuthenticated ? this.RedirectToAction("Index", "Home") : this.View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginRequest model)
        {
            if (!this.ModelState.IsValid)
            {
                return this.View(model);
            }

            try
            {
                string? currentIpAddress = this._httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
                if (string.IsNullOrEmpty(currentIpAddress))
                {
                    this._logManager.Warning("Login failed: Unable to retrieve client IP address.");
                    this.ModelState.AddModelError("", "Login failed due to a network issue. Please try again.");
                    return this.View(model);
                }

                AuthResponse response = await this._authService.LoginAsync(model, currentIpAddress);

                this._logManager.Information($"UserLogin {model.Username} successfully logged in.");
                return this.RedirectToAction("Index", "Home");
            }
            catch (UserFriendlyExceptionBase ex)
            {
                this._logManager.Warning($"Login failed for {model.Username}: {ex.Message}");
                this.ModelState.AddModelError("", ex.Message);
                return this.View(model);
            }
            catch (Exception ex)
            {
                this._logManager.Error($"Unexpected error during login for {model.Username}.", exception: ex);
                this.ModelState.AddModelError("", "An unexpected error occurred during login. Please try again later.");
                return this.View(model);
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            return this._userContextService.IsAuthenticated ? this.RedirectToAction("Index", "Home") : this.View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterRequest model)
        {
            RecaptchaSettings recaptchaSettings = this._recaptchaSettings;
            bool devMode = await this._configSingleton.GetDevelopmentModeAsync();
            bool isUsingTestKeys = devMode && recaptchaSettings.SiteKey == "6LeIxAcTAAAAAJcZVRqyHh71UMIEGNQ_MXjiZKhI";

            if (string.IsNullOrEmpty(model.RecaptchaResponse) && this.Request.Form.ContainsKey("g-recaptcha-response"))
            {
                model.RecaptchaResponse = this.Request.Form["g-recaptcha-response"];
                _ = this.ModelState.Remove("RecaptchaResponse");
            }

            if (!this.ModelState.IsValid)
            {
                return this.View(model);
            }

            if (!isUsingTestKeys)
            {
                RecaptchaResponse? validationResponse = null;
                if (!string.IsNullOrEmpty(model.RecaptchaResponse))
                {
                    validationResponse = await this._recaptchaService.Validate(model.RecaptchaResponse);
                }

                if (validationResponse == null || !validationResponse.success)
                {
                    this._logManager.Warning("reCAPTCHA validation failed for user registration attempt.");
                    this.ModelState.AddModelError("RecaptchaResponse", "reCAPTCHA verification failed. Please try again.");
                    return this.View(model);
                }
            }
            else if (!string.IsNullOrEmpty(model.RecaptchaResponse))
            {
                this._logManager.Information("Accepting reCAPTCHA test response during registration.");
            }
            else
            {
                this.ModelState.AddModelError("RecaptchaResponse", "reCAPTCHA response missing (Test Mode).");
                return this.View(model);
            }

            try
            {
                string? ipAddress = this._httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
                if (string.IsNullOrEmpty(ipAddress))
                {
                    this._logManager.Error("Registration failed: Unable to retrieve client IP address.");
                    throw new RedUserException("Unable to retrieve your IP address. Registration cannot proceed.");
                }

                AuthResponse response = await this._authService.RegisterAsync(model, ipAddress);

                this._logManager.Information($"UserLogin {model.Username} successfully registered and logged in.");
                return this.RedirectToAction("Index", "Home");
            }
            catch (UserFriendlyExceptionBase ex)
            {
                this._logManager.Warning($"Registration failed for {model.Username}: {ex.Message}");
                this.ModelState.AddModelError("", ex.Message);
                return this.View(model);
            }
            catch (Exception ex)
            {
                this._logManager.Error($"Unexpected error during registration for {model.Username}.", exception: ex);
                this.ModelState.AddModelError("", "An unexpected error occurred during registration. Please try again later.");
                return this.View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await this._authService.LogoutAsync();
            this._logManager.Information($"UserLogin logged out: {this._userContextService.Username}");
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
            if (!this._userContextService.IsAuthenticated || this._userContextService.UserId == null)
            {
                return this.RedirectToAction("Login");
            }

            UserGet? userGet = await this._userRepository.GetUserByIdAsync(this._userContextService.UserId.Value);
            if (userGet == null)
            {
                return this.NotFound("UserLogin profile not found.");
            }
            PrivateProfileViewModel viewModel = new()
            {
                RegistrationDate = userGet.RegistrationDate,
                Tier = userGet.Tier,
                Username = userGet.Username,
                ChangeUsernameModel = new ChangeUsernameViewModel { NewUsername = userGet.Username },
                ChangePasswordModel = new ChangePasswordViewModel()
            };

            return this.View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> PublicProfile(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return this.RedirectToAction("Index", "Home");
            }

            UserGet? userGet = await this._userRepository.GetUserByUsernameAsync(username);
            if (userGet == null)
            {
                return this.NotFound();
            }

            string? currentUserId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            bool isOwnProfile = currentUserId != null && int.TryParse(currentUserId, out int id) && userGet.Id == id;

            bool isExplicitPublicProfileRequest = this.Request.Headers["Referer"].ToString().Contains("/PrivateProfile");

            if (isOwnProfile && !isExplicitPublicProfileRequest && !this.Request.Query.ContainsKey("viewPublic"))
            {
                return this.RedirectToAction("PrivateProfile");
            }

            IEnumerable<MapLocationGet> userLocations = await this._mapLocation.GetLocationsByUserIdAsync(userGet.Id);

            IEnumerable<DiscussionThreadGet> userThreads = await this._discussionRepository.GetThreadsByUserIdAsync(userGet.Id);

            bool isAdmin = false;
            if (currentUserId != null && int.TryParse(currentUserId, out int currentId))
            {
                UserGet? currentUserGet = await this._userRepository.GetUserByIdAsync(currentId);
                isAdmin = currentUserGet != null && currentUserGet.Tier == UserTier.Admin;
            }

            UserBanGet? activeBan = await this._userRepository.GetActiveBanByUserIdAsync(userGet.Id);

            if (activeBan == null || !activeBan.IsActive)
            {
                string? registrationIp = userGet.IpAddressHistory?.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                if (!string.IsNullOrEmpty(registrationIp))
                {
                    activeBan = await this._userRepository.GetActiveBanByIpAddressAsync(registrationIp);
                }
            }

            PublicProfileViewModel model = new()
            {
                UserId = userGet.Id,
                Username = userGet.Username,
                Tier = userGet.Tier,
                RegistrationDate = userGet.RegistrationDate,
                UserLocations = userLocations,
                UserThreads = userThreads,
                IsAdmin = isAdmin,
                CurrentBan = activeBan
            };

            return this.View(model);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> BanUser(string username)
        {
            string? currentUserId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId) || !int.TryParse(currentUserId, out int adminId))
            {
                return this.Forbid();
            }

            UserGet? adminGet = await this._userRepository.GetUserByIdAsync(adminId);
            if (adminGet == null || adminGet.Tier != UserTier.Admin)
            {
                return this.Forbid();
            }

            UserGet? userGet = await this._userRepository.GetUserByUsernameAsync(username);
            if (userGet == null)
            {
                return this.NotFound();
            }

            this.ViewBag.Username = userGet.Username;
            this.ViewBag.UserId = userGet.Id;
            string registrationIp = userGet.IpAddressHistory?.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "N/A";
            this.ViewBag.RegistrationIp = registrationIp;
            this.ViewBag.UserTier = userGet.Tier;

            return this.View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ProcessBan(int userId, string username, BanViewModel model)
        {
            string? currentUserId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId) || !int.TryParse(currentUserId, out int adminId))
            {
                return this.Forbid();
            }

            UserGet? adminGet = await this._userRepository.GetUserByIdAsync(adminId);
            if (adminGet == null || adminGet.Tier != UserTier.Admin)
            {
                return this.Forbid();
            }

            UserGet? userGet = await this._userRepository.GetUserByIdAsync(userId);
            if (userGet == null)
            {
                return this.NotFound();
            }

            if (userGet.Tier == UserTier.Admin)
            {
                this.TempData["ErrorMessage"] = "Cannot ban another administrator.";
                return this.RedirectToAction("PublicProfile", new { username });
            }

            if (!this.ModelState.IsValid)
            {
                this.TempData["ErrorMessage"] = "Invalid ban parameters.";
                return this.RedirectToAction("BanUser", new { username });
            }

            UserBanGetCreate ban = new()
            {
                BannedByUserId = adminId,
                Reason = model.Reason,
                BanType = model.BanType,
                BannedAt = DateTime.UtcNow
            };

            if (!model.IsPermanent && model.BanDurationDays.HasValue)
            {
                ban.ExpiresAt = DateTime.UtcNow.AddDays(model.BanDurationDays.Value);
            }

            if (model.BanType == BanType.Account)
            {
                ban.UserId = userId;
            }
            else if (model.BanType == BanType.IpAddress)
            {
                string? registrationIp = null;
                if (!string.IsNullOrEmpty(userGet.IpAddressHistory))
                {
                    registrationIp = userGet.IpAddressHistory?.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                }

                if (string.IsNullOrEmpty(registrationIp))
                {
                    this.TempData["ErrorMessage"] = "Could not determine registration IP for IP ban.";
                    return this.RedirectToAction("PublicProfile", new { username });
                }

                ban.HashedIpAddress = registrationIp;
            }

            int banId = await this._userRepository.BanUserAsync(ban);

            if (banId > 0)
            {
                this.TempData["SuccessMessage"] = model.BanType == BanType.Account
                    ? $"UserLogin {userGet.Username} has been banned successfully."
                    : $"IP address ban applied successfully.";
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
            string? currentUserId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId) || !int.TryParse(currentUserId, out int adminId))
            {
                return this.Forbid();
            }

            UserGet? adminGet = await this._userRepository.GetUserByIdAsync(adminId);
            if (adminGet == null || adminGet.Tier != UserTier.Admin)
            {
                return this.Forbid();
            }

            bool success = await this._userRepository.UnbanUserAsync(banId);

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
                MapHive.Models.ViewModels.PrivateProfileViewModel? profileModel = await this.GetCurrentUserPrivateProfileAsync();
                return profileModel == null ? this.RedirectToAction("Login") : this.View("PrivateProfile", profileModel);
            }

            string? userId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null || !int.TryParse(userId, out int id))
            {
                return this.RedirectToAction("Login");
            }

            UserGet? userGet = await this._userRepository.GetUserByIdAsync(id);
            if (userGet == null)
            {
                return this.RedirectToAction("Login");
            }

            if (await this._userRepository.CheckUsernameExistsAsync(model.NewUsername) && !userGet.Username.Equals(model.NewUsername, StringComparison.OrdinalIgnoreCase))
            {
                this.ModelState.AddModelError("ChangeUsernameModel.NewUsername", "Username already exists");
                PrivateProfileViewModel? profileModel = await this.GetCurrentUserPrivateProfileAsync();
                return profileModel == null ? this.RedirectToAction("Login") : this.View("PrivateProfile", profileModel);
            }

            userGet.Username = model.NewUsername;
            UserUpdate updateDto = this._mapper.Map<UserUpdate>(userGet);
            _ = await this._userRepository.UpdateUserAsync(updateDto);

            List<Claim> claims = new()
            {
                new Claim(ClaimTypes.Name, userGet.Username),
                new Claim(ClaimTypes.NameIdentifier, userGet.Id.ToString()),
                new Claim("UserTier", ((int)userGet.Tier).ToString()),
            };

            this.AddAdminRoleClaim(claims, userGet.Tier);

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
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!this.ModelState.IsValid)
            {
                MapHive.Models.ViewModels.PrivateProfileViewModel? profileModel = await this.GetCurrentUserPrivateProfileAsync();
                return profileModel == null ? this.RedirectToAction("Login") : this.View("PrivateProfile", profileModel);
            }

            string? userId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null || !int.TryParse(userId, out int id))
            {
                return this.RedirectToAction("Login");
            }

            UserGet? userGet = await this._userRepository.GetUserByIdAsync(id);
            if (userGet == null)
            {
                return this.RedirectToAction("Login");
            }

            if (!this._authService.VerifyPassword(model.CurrentPassword, userGet.PasswordHash))
            {
                this.ModelState.AddModelError("ChangePasswordModel.CurrentPassword", "Current password is incorrect");
                PrivateProfileViewModel? profileModel = await this.GetCurrentUserPrivateProfileAsync();
                return profileModel == null ? this.RedirectToAction("Login") : this.View("PrivateProfile", profileModel);
            }

            UserUpdate userUpdate = this._mapper.Map<UserUpdate>(userGet);
            userUpdate.PasswordHash = this._authService.HashPassword(model.NewPassword);
            _ = await this._userRepository.UpdateUserAsync(userUpdate);

            this.TempData["SuccessMessage"] = "Password changed successfully";
            return this.RedirectToAction("PrivateProfile");
        }

        private async Task<MapHive.Models.ViewModels.PrivateProfileViewModel?> GetCurrentUserPrivateProfileAsync()
        {
            string? userId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null || !int.TryParse(userId, out int id))
            {
                return null;
            }

            UserGet? userGet = await this._userRepository.GetUserByIdAsync(id);
            if (userGet == null)
            {
                return null;
            }

            IEnumerable<MapLocationGet> userLocations = await this._mapLocation.GetLocationsByUserIdAsync(id);

            IEnumerable<DiscussionThreadGet> userThreads = await this._discussionRepository.GetThreadsByUserIdAsync(id);

            return new MapHive.Models.ViewModels.PrivateProfileViewModel
            {
                Username = userGet.Username,
                Tier = userGet.Tier,
                RegistrationDate = userGet.RegistrationDate,
                UserLocations = userLocations,
                UserThreads = userThreads
            };
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