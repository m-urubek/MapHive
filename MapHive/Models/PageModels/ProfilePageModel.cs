namespace MapHive.Models.PageModels;

using System.ComponentModel.DataAnnotations;
using MapHive.Models.Data.DbTableModels;
using MapHive.Models.Enums;

// Base profile view model with shared properties
public abstract class IBaseProfilePageModel
{
    public required string Username { get; set; }
    public required int AccountId { get; set; }
    public required AccountTier Tier { get; set; }
    public required DateTime RegistrationDate { get; set; }
    public required IEnumerable<LocationExtended> UserLocations { get; set; }
    public required IEnumerable<ThreadInitialMessageDbModel> UserThreads { get; set; }
    public required BanOnProfilePageModel? CurrentBan { get; set; }
}

// Private profile - for the logged in user viewing their own profile
public class PrivateProfilePageModel : IBaseProfilePageModel
{
    // For changing username
    public required ChangeUsernamePageModel ChangeUsernamePageModel { get; set; }

    // For changing password
    public required ChangePasswordPageModel ChangePasswordPageModel { get; set; }
}

// Public profile - for viewing another user's profile
public class PublicProfilePageModel : IBaseProfilePageModel
{
    public required bool SignedInUserIsAdmin { get; set; }
}

public class ChangeUsernamePageModel
{
    [Required]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
    public string? NewUsername { get; set; }
}

public class ChangePasswordPageModel
{
    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Current Password")]
    public string? CurrentPassword { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters")]
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    public string? NewPassword { get; set; }

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm New Password")]
    [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
    public string? ConfirmNewPassword { get; set; }
}
