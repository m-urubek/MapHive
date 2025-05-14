namespace MapHive.Models.ViewModels
{
    using System.ComponentModel.DataAnnotations;
    using MapHive.Models.Enums;
    using MapHive.Models.RepositoryModels;

    // Base profile view model with shared properties
    public abstract class IBaseProfileViewModel
    {
        public required string Username { get; set; }
        public required int AccountId { get; set; }
        public required AccountTier Tier { get; set; }
        public required DateTime RegistrationDate { get; set; }
        public required IEnumerable<MapLocationGet> UserLocations { get; set; }
        public required IEnumerable<DiscussionThreadGet> UserThreads { get; set; }
        public BanViewModel? CurrentBan { get; set; }
    }

    // Private profile - for the logged in user viewing their own profile
    public class PrivateProfileViewModel : IBaseProfileViewModel
    {
        // For changing username
        public ChangeUsernameViewModel ChangeUsernameViewModel { get; set; } = new ChangeUsernameViewModel();

        // For changing password
        public ChangePasswordViewModel ChangePasswordViewModel { get; set; } = new ChangePasswordViewModel();
    }

    // Public profile - for viewing another user's profile
    public class PublicProfileViewModel : IBaseProfileViewModel
    {
        
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
