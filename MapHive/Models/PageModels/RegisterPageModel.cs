namespace MapHive.Models.PageModels;

using System.ComponentModel.DataAnnotations;
using MapHive.Attributes;

public class RegisterPageModel
{
    [Required]
    [StringLength(40)]
    [NoHtml]
    [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Username can only contain letters, numbers, and underscores.")]
    public string? Username { get; set; }

    [Required]
    [DataType(DataType.Password)]
    public string? Password { get; set; }

    [Required]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string? ConfirmPassword { get; set; }

    [Required]
    public string? RecaptchaResponse { get; set; }
}
