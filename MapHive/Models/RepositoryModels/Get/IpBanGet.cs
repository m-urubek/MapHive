namespace MapHive.Models.RepositoryModels
{
    public class IpBanGet
    {
        public required int Id { get; set; }
        public required string HashedIpAddress { get; set; }
        public required string Reason { get; set; }
        public required DateTime BannedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public required int BannedByUserId { get; set; }
        public required bool IsActive { get; set; }
        public required Dictionary<string, string> Properties { get; set; }
    }
}