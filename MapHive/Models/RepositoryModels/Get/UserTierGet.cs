using MapHive.Models.Enums;

namespace MapHive.Models.RepositoryModels
{
    public class UserTierGet
    {
        public int UserId { get; set; }
        public UserTier Tier { get; set; }
    }
}