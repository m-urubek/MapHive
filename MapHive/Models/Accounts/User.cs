using System.ComponentModel.DataAnnotations;

namespace MapHive.Models
{
    public enum UserTier
    {
        Normal = 0,
        Trusted = 1,
        Admin = 2
    }

    public class User
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public DateTime RegistrationDate { get; set; }

        [Required]
        public string IpAddress { get; set; } = string.Empty;

        public UserTier Tier { get; set; } = UserTier.Normal;

    }
}