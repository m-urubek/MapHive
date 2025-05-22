namespace MapHive.Models.PageModels;

using System.ComponentModel.DataAnnotations;

public class LoginPageModel
{
    [Required]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
    public required string Username { get; set; }

    [Required]
    [DataType(DataType.Password)]
    public required string Password { get; set; }
}