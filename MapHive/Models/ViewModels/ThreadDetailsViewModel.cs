namespace MapHive.Models.ViewModels
{
    using MapHive.Models.RepositoryModels;

    public class ThreadDetailsViewModel
    {
        public required int Id { get; set; }
        public required int LocationId { get; set; }
        public required string ThreadName { get; set; }
        public required bool IsReviewThread { get; set; }
        public int? ReviewId { get; set; }
        public required DateTime CreatedAt { get; set; }
        public required string AuthorUsername { get; set; }
        public required int AuthorId { get; set; }
        public required List<ThreadMessageGet> Messages { get; set; }
        public required bool IsAnonymous { get; set; }

        // Navigation properties
        public required MapLocationGet Location { get; set; }
        public ReviewGet? Review { get; set; }

        // Helpers for view rendering
        public bool HasInitialMessage => InitialMessage != null;
        public ThreadMessageGet InitialMessage => Messages.FirstOrDefault(predicate: m => m.IsInitialMessage) ?? throw new Exception($"Thread \"{Id}\" does not have initial message");
    }
}
