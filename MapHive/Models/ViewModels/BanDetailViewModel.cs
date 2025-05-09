namespace MapHive.Models
{
    using MapHive.Models.RepositoryModels;

    public class BanDetailViewModel
    {
        public UserBanGet Ban { get; set; } = new UserBanGet();
        public string BannedUsername { get; set; } = string.Empty;
        public string BannedByUsername { get; set; } = string.Empty;

        public string BanStatus => Ban.IsActive ? "Active" : "Expired";
        public string BanTypeDisplay => Ban.BanType.ToString();
        public string FormattedExpiresAt => Ban.ExpiresAt.HasValue ? Ban.ExpiresAt.Value.ToString(format: "g") : "Never";
    }
}