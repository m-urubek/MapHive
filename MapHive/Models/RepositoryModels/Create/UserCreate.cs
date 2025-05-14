namespace MapHive.Models.RepositoryModels
{
    using System.ComponentModel.DataAnnotations;
    using MapHive.Models.Enums;

    public class UserCreate
    {
        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
        public required string Username { get; set; }

        [Required(ErrorMessage = "Password hash is required")]
        [DataType(DataType.Password)]
        public required string PasswordHash { get; set; }

        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;

        [Required(ErrorMessage = "UserLogin tier is required")]
        public AccountTier Tier { get; set; }

        public string IpAddressHistory { get; set; } = string.Empty;
    }
}