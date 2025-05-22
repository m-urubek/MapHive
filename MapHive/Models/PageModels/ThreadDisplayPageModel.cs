namespace MapHive.Models.PageModels;

using MapHive.Models.Data.DbTableModels;

public class ThreadDisplayPageModel
{
    public required int Id { get; set; }
    public required int LocationId { get; set; }
    public required string LocationName { get; set; }
    public required int? AuthorId { get; set; }
    public required string AuthorUsername { get; set; }
    public required string ThreadName { get; set; }
    public int? ReviewId { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required int? Rating { get; set; }

    public required bool IsAnonymous { get; set; }
    public required List<ThreadMessageExtended> Messages { get; set; }

    public ThreadMessageExtended InitialMessage => Messages.FirstOrDefault(predicate: m => m.IsInitialMessage) ?? throw new Exception($"Thread \"{Id}\" does not have initial message");
}
