namespace MapHive.Models.PageModels;

using MapHive.Models.Enums;

public class BanOnProfilePageModel
{
    public required BanType BanType { get; set; }
    public required string? Reason { get; set; }
    public required DateTime? ExpiresAt { get; set; }
    public required int BannedById { get; set; }
    public required string CreatedDateTime { get; set; }
    public required string BannedByUsername { get; set; }
}
