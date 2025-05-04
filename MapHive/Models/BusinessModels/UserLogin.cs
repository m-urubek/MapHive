using MapHive.Models.Enums;

namespace MapHive.Models.BusinessModels
{
    public class UserLogin
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public UserTier Tier { get; set; }
    }
}