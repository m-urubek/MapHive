namespace MapHive.Models.RepositoryModels
{
    public class IpBanGet
    {
        public required int Id { get; set; }
        public required string HashedIpAddress { get; set; }
        public string? Reason { get; set; }
        public required DateTime BannedAt { get; set; }
        public required int BannedByAccountId { get; set; }
    }
}