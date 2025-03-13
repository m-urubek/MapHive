using System.ComponentModel.DataAnnotations;

namespace MapHive.Models
{
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

        [Required]
        public string MacAddress { get; set; } = string.Empty;

        public bool IsTrusted { get; set; }

        public bool IsAdmin { get; set; }
    }
}