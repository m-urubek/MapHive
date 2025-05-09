namespace MapHive.Models.RepositoryModels
{
    using MapHive.Models.Enums;

    public class UserBanGet
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string? HashedIpAddress { get; set; }
        public int BannedByUserId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public BanType BanType { get; set; }
        public DateTime BannedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsActive { get; set; }
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
    }
}