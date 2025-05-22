namespace MapHive.Models.PageModels;
public class MessageDispPageModel
{
    public required int Id { get; set; }
    public required int? AuthorId { get; set; }
    public required string AuthorUsername { get; set; }
    public required string? MessageText { get; set; }
    public required bool IsInitialMessage { get; set; }
    public required int? DeletedByAccountId { get; set; }
    public required int? DeletedByUsername { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required DateTime? DeletedAt { get; set; }
}
