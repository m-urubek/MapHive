namespace MapHive.Models.RepositoryModels
{
    using MapHive.Models.Enums;

    public class UserGet
    {
        public required int Id { get; set; }
        public required string Username { get; set; }
        public required string PasswordHash { get; set; }
        public required UserTier Tier { get; set; }
        public required DateTime RegistrationDate { get; set; }
        public required string IpAddressHistory { get; set; }
    }
}
