using System.ComponentModel.DataAnnotations;

namespace MapHive.Models
{
    public class ProfileViewModel
    {
        public string Username { get; set; } = string.Empty;
        public bool IsTrusted { get; set; }
        public DateTime RegistrationDate { get; set; }

        // For changing username
        public ChangeUsernameViewModel ChangeUsernameModel { get; set; } = new ChangeUsernameViewModel();

        // For changing password
        public ChangePasswordViewModel ChangePasswordModel { get; set; } = new ChangePasswordViewModel();

        // User's locations
        public IEnumerable<MapLocation> UserLocations { get; set; } = new List<MapLocation>();
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
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}