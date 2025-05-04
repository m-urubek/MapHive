using MapHive.Models.Enums;

namespace MapHive.Models.RepositoryModels
{
    public class UserGet
    {
        public int Id { get; set; }
        public required string Username { get; set; }
        public string PasswordHash { get; set; } = string.Empty;
        public UserTier Tier { get; set; }
        public DateTime RegistrationDate { get; set; }
        public string IpAddressHistory { get; set; } = string.Empty;
    }
}