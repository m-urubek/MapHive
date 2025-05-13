namespace MapHive.Models.RepositoryModels
{
    using MapHive.Models.Enums;

    public class UserBanGet
    {
        public required int Id { get; set; }
        public int? UserId { get; set; }
        public string? HashedIpAddress { get; set; } //todo ip address shouldnt be in user ban table
        public required int BannedByUserId { get; set; }
        public required string Reason { get; set; }
        public required BanType BanType { get; set; }
        public required DateTime BannedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public required bool IsActive { get; set; }
        public required Dictionary<string, string> Properties { get; set; }
    }
}