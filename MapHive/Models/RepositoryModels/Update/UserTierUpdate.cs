namespace MapHive.Models.RepositoryModels
{
    using MapHive.Models.Enums;

    public class UserTierUpdate
    {
        public int UserId { get; set; }
        public UserTier Tier { get; set; }
    }
}