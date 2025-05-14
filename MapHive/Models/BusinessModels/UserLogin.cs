namespace MapHive.Models.BusinessModels
{
    using MapHive.Models.Enums;

    public class UserLogin
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public AccountTier Tier { get; set; }
    }
}