using System.ComponentModel.DataAnnotations;

namespace MapHive.Models
{
    public enum BanType
    {
        Account = 0,
        IpAddress = 1
    }

    public class UserBan
    {
        public int Id { get; set; }

        // For account bans
        public int? UserId { get; set; }

        // For IP bans - Stored as SHA256 hash
        public string? HashedIpAddress { get; set; }

        [Required]
        public BanType BanType { get; set; }

        [Required]
        public DateTime BannedAt { get; set; } = DateTime.UtcNow;

        // If null, the ban is permanent
        public DateTime? ExpiresAt { get; set; }

        [Required]
        public string Reason { get; set; } = string.Empty;

        // Admin who issued the ban
        [Required]
        public int BannedByUserId { get; set; }

        // Calculated property
        public bool IsActive => this.ExpiresAt == null || this.ExpiresAt > DateTime.UtcNow;

        // Additional properties for display purposes (not stored in the database)
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
    }

    public class BanViewModel
    {
        [Required]
        public BanType BanType { get; set; }

        // True = permanent, False = temporary
        [Required]
        public bool IsPermanent { get; set; }

        // Only used if IsPermanent is false
        public int? BanDurationDays { get; set; }

        [Required]
        public string Reason { get; set; } = string.Empty;
    }
}