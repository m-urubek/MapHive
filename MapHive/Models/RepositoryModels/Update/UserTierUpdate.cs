using MapHive.Models.Enums;

namespace MapHive.Models.RepositoryModels
{
    public class UserTierUpdate
    {
        public int UserId { get; set; }
        public UserTier Tier { get; set; }
    }
}