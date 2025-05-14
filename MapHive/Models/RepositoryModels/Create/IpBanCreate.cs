namespace MapHive.Models.RepositoryModels
{
    public class IpBanCreate
    {
        // SHA256-hashed IP address
        public required string HashedIpAddress { get; set; }

        // Reason for ban
        public string? Reason { get; set; }

        // Date when the address was banned
        public required DateTime BannedAt { get; set; }

        // ID of the user who performed the IP ban
        public required int BannedByAccountId { get; set; }

        // Optional expiration date for the ban
        public DateTime? ExpiresAt { get; set; }
    }
}