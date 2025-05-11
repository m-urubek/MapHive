namespace MapHive.Models.ViewModels
{
    using System.Collections.Generic;
    using MapHive.Models.RepositoryModels;

    public class BanDetailViewModel
    {
        public required UserBanGet Ban { get; set; }
        public required string BannedUsername { get; set; }
        public required string BannedByUsername { get; set; }

        public string BanStatus => Ban.IsActive ? "Active" : "Expired";
        public string BanTypeDisplay => Ban.BanType.ToString();
        public string FormattedExpiresAt => Ban.ExpiresAt.HasValue ? Ban.ExpiresAt.Value.ToString(format: "g") : "Never";
    }
}