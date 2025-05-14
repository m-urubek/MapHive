namespace MapHive.Models.ViewModels
{
    using MapHive.Models.RepositoryModels;

    public class BanDetailViewModel
    {
        public required string BannedUsername { get; set; }
        public required string BannedByUsername { get; set; }

        public required bool IsActive { get; set; }
        public required string BanTypeDisplay { get; set; }
        public required string FormattedExpiresAt { get; set; }
    }
}
