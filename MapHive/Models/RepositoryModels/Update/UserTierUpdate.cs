namespace MapHive.Models.RepositoryModels
{
    using MapHive.Models.Enums;

    public class AccountTierUpdate
    {
        public int AccountId { get; set; }
        public AccountTier Tier { get; set; }
    }
}