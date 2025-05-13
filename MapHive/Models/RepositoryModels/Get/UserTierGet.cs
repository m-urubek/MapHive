namespace MapHive.Models.RepositoryModels
{
    using MapHive.Models.Enums;

    public class UserTierGet
    {
        public required int UserId { get; set; }
        public required UserTier Tier { get; set; }
    }
}