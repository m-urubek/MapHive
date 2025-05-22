namespace MapHive.Models.Data.DbTableModels;
public class ThreadMessageExtended
{
    public required int Id { get; set; }

    public required int ThreadId { get; set; }

    public required int? AuthorId { get; set; }

    public required string MessageText { get; set; }

    public required bool IsInitialMessage { get; set; }

    public required int? DeletedByAccountId { get; set; }

    public required DateTime CreatedAt { get; set; }

    public required DateTime? DeletedAt { get; set; }

    public required string AuthorUsername { get; set; }

    public required string? DeletedByUsername { get; set; }
}
