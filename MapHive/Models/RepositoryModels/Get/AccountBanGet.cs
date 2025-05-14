namespace MapHive.Models.RepositoryModels
{
    using MapHive.Models.Enums;

    public class AccountBanGet
    {
        public required int Id { get; set; }
        public required int? AccountId { get; set; }
        public required string? HashedIpAddress { get; set; } //todo ip address shouldnt be in user ban table
        public required int BannedByAccountId { get; set; }
        public required string? Reason { get; set; }
        public required DateTime BannedAt { get; set; }
        public required DateTime? ExpiresAt { get; set; }

        public bool IsActive => ExpiresAt == null || ExpiresAt > DateTime.UtcNow;
    }
}
