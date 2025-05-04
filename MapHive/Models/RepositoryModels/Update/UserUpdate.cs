using MapHive.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace MapHive.Models.RepositoryModels
{
    public class UserUpdate
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [StringLength(50, MinimumLength = 3)]
        public required string Username { get; set; }

        [DataType(DataType.Password)]
        public string? PasswordHash { get; set; }

        [Required]
        public UserTier Tier { get; set; }

        public string IpAddressHistory { get; set; } = string.Empty;
    }
}