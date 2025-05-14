namespace MapHive.Models.RepositoryModels
{
    using MapHive.Models.Enums;

    public class AccountGet
    {
        public required int Id { get; set; }
        public required string Username { get; set; }
        public required string PasswordHash { get; set; }
        public required AccountTier Tier { get; set; }
        public required DateTime RegistrationDate { get; set; }
        public required string IpAddressHistory { get; set; }
    }
}
