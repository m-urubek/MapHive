namespace MapHive.Models.RepositoryModels
{
    public class IpBanCreate
    {
        // SHA256-hashed IP address
        public required string IpAddress { get; set; }

        // Reason for ban
        public required string Reason { get; set; }

        // Date when the address was banned
        public DateTime BannedAt { get; set; } = DateTime.UtcNow;

        // ID of the user who performed the IP ban
        public required int BannedByUserId { get; set; }

        // Optional expiration date for the ban
        public DateTime? ExpiresAt { get; set; }
    }
}