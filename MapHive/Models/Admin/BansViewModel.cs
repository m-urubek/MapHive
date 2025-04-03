namespace MapHive.Models
{
    public class BansViewModel
    {
        public IEnumerable<UserBan> Bans { get; set; } = Enumerable.Empty<UserBan>();
        public string SearchTerm { get; set; } = string.Empty;
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalCount { get; set; } = 0;
        public int TotalPages => (int)Math.Ceiling(this.TotalCount / (double)this.PageSize);
    }

    public class BanDetailViewModel
    {
        public UserBan Ban { get; set; } = new UserBan();
        public string BannedUsername { get; set; } = string.Empty;
        public string BannedByUsername { get; set; } = string.Empty;
        public string FormattedExpiryDate => this.Ban.ExpiresAt.HasValue ? this.Ban.ExpiresAt.Value.ToString("g") : "Never (Permanent)";
        public string BanStatus => this.Ban.IsActive ? "Active" : "Expired";
        public string BanTypeDisplay => this.Ban.BanType == BanType.Account ? "Account Ban" : "IP Address Ban";
    }
}