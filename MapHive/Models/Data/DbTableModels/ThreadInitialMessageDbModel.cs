namespace MapHive.Models.Data.DbTableModels;
public class ThreadInitialMessageDbModel
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
    public required string InitialMessageText { get; set; }
    public required DateTime? InitialMessageDeletedAt { get; set; }
    public required int? InitialMessageDeletedByAccountId { get; set; }
    public required string? InitialMessageDeletedByUsername { get; set; }
}
