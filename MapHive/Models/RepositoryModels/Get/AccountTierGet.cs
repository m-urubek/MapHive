namespace MapHive.Models.RepositoryModels
{
    using MapHive.Models.Enums;

    public class AccountTierGet
    {
        public required int AccountId { get; set; }
        public required AccountTier Tier { get; set; }
    }
}