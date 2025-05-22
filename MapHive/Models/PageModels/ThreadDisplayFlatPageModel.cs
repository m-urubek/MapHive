namespace MapHive.Models.PageModels;
public class ThreadDisplayFlatPageModel
{
    public required int Id { get; set; }
    public required int LocationId { get; set; }
    public required int? AuthorId { get; set; }
    public required string AuthorUsername { get; set; }
    public required string ThreadName { get; set; }
    public int? ReviewId { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required bool IsAnonymous { get; set; }
    public required int MessagesCount { get; set; }
}
