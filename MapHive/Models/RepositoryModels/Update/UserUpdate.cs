namespace MapHive.Models.RepositoryModels
{
    using System.ComponentModel.DataAnnotations;
    using MapHive.Models.Enums;

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
        public AccountTier Tier { get; set; }

        public string IpAddressHistory { get; set; } = string.Empty;
    }
}