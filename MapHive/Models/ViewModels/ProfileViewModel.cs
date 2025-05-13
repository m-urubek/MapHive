namespace MapHive.Models.ViewModels
{
    using System.ComponentModel.DataAnnotations;
    using MapHive.Models.Enums;
    using MapHive.Models.RepositoryModels;

    // Base profile view model with shared properties
    public class BaseProfileViewModel
    {
        public string Username { get; set; } = string.Empty;
        public UserTier Tier { get; set; }
        public DateTime RegistrationDate { get; set; }

        // UserLogin's locations
        public IEnumerable<MapLocationGet> UserLocations { get; set; } = new List<MapLocationGet>();

        // UserLogin's threads
        public IEnumerable<DiscussionThreadGet> UserThreads { get; set; } = new List<DiscussionThreadGet>();
    }

    // Private profile - for the logged in user viewing their own profile
    public class PrivateProfileViewModel : BaseProfileViewModel
    {
        // For changing username
        public ChangeUsernameViewModel ChangeUsernameViewModel { get; set; } = new ChangeUsernameViewModel();

        // For changing password
        public ChangePasswordViewModel ChangePasswordViewModel { get; set; } = new ChangePasswordViewModel();
    }

    // Public profile - for viewing another user's profile
    public class PublicProfileViewModel : BaseProfileViewModel
    {
        // UserLogin ID of the profile owner
        public int UserId { get; set; }

        // Ban information (if the user is banned)
        public UserBanGet? CurrentBan { get; set; }

        // For admins to ban the user
        public bool IsAdmin { get; set; }
    }

    // For backward compatibility
    public class ProfileViewModel : PrivateProfileViewModel
    {
        // This class inherits all properties from PrivateProfileViewModel
    }

    public class ChangeUsernameViewModel
    {
        [Required]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
        public string NewUsername { get; set; } = string.Empty;
    }

    public class ChangePasswordViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters")]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}