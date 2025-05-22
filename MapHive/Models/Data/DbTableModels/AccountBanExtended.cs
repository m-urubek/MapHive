namespace MapHive.Models.Data.DbTableModels;

public class AccountBanExtended
{
    public required int Id { get; set; }
    public required int AccountId { get; set; }
    public required int BannedByAccountId { get; set; }
    public required string BannedByUsername { get; set; }
    public required string? Reason { get; set; }
    public required DateTime CreatedDateTime { get; set; }
    public required DateTime? ExpiresAt { get; set; }
}
