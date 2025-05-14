namespace MapHive.Models.ViewModels
{
    using MapHive.Models.Enums;

    public class BanUserPageViewModel
    {
        public required string Username { get; set; }
        public int AccountId { get; set; }

        public AccountTier AccountTier { get; set; }
    }
}
