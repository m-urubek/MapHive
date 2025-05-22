namespace MapHive.Models.PageModels;

/// <summary>used for create and update, view needs own</summary>
public class ReviewDisplayPageModel
{
    public required int Id { get; set; }
    public required int Rating { get; set; }
    public required string ReviewText { get; set; }
    public required bool IsAnonymous { get; set; }
    public required string AuthorUsername { get; set; }
    public required int? AuthorId { get; set; }
    public required DateTime CreatedAt { get; set; }
}
