namespace MapHive.Models.RepositoryModels
{
    using MapHive.Models.Enums;

    public class AccountBanCreate
    {
        public required int AccountId { get; set; }
        public required int BannedByAccountId { get; set; }
        public string? Reason { get; set; }
        public required DateTime BannedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
