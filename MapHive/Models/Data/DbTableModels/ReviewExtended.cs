namespace MapHive.Models.Data.DbTableModels;

public class ReviewExtended
{
    public required int Id { get; set; }
    public required int LocationId { get; set; }
    public required int? AccountId { get; set; }
    public required string AuthorUsername { get; set; }
    public required int Rating { get; set; }
    public required bool IsAnonymous { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required DateTime UpdatedAt { get; set; }
    public required string ReviewText { get; set; }
}
