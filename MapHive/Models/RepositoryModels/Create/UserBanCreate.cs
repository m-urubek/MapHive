namespace MapHive.Models.RepositoryModels
{
    using MapHive.Models.Enums;

    public class UserBanGetCreate
    {
        public int? UserId { get; set; }
        public string? HashedIpAddress { get; set; }
        public int BannedByUserId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public BanType BanType { get; set; }
        public DateTime BannedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }
    }
}