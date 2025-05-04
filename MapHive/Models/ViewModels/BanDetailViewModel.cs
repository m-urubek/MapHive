using MapHive.Models.RepositoryModels;

namespace MapHive.Models
{
    public class BanDetailViewModel
    {
        public UserBanGet Ban { get; set; } = new UserBanGet();
        public string BannedUsername { get; set; } = string.Empty;
        public string BannedByUsername { get; set; } = string.Empty;

        public string BanStatus => this.Ban.IsActive ? "Active" : "Expired";
        public string BanTypeDisplay => this.Ban.BanType.ToString();
        public string FormattedExpiresAt => this.Ban.ExpiresAt.HasValue ? this.Ban.ExpiresAt.Value.ToString("g") : "Never";
    }
}