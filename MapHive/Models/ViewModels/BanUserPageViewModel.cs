namespace MapHive.Models.ViewModels
{
    using MapHive.Models.Enums;

    public class BanUserPageViewModel : BanViewModel
    {
        public string Username { get; set; } = string.Empty;

        public int UserId { get; set; }

        public UserTier UserTier { get; set; }

        public string RegistrationIp { get; set; } = string.Empty;
    }
}