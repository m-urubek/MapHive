namespace MapHive.Models.ViewModels
{
    using MapHive.Models.RepositoryModels;

    public class ThreadDetailsViewModel
    {
        public int Id { get; set; }
        public int LocationId { get; set; }
        public required string ThreadName { get; set; }
        public bool IsReviewThread { get; set; }
        public int? ReviewId { get; set; }
        public DateTime CreatedAt { get; set; }
        public required string AuthorName { get; set; }
        public List<ThreadMessageGet> Messages { get; set; } = new List<ThreadMessageGet>();

        // Navigation properties
        public required MapLocationGet Location { get; set; }
        public ReviewGet? Review { get; set; }

        // Helpers for view rendering
        public bool HasInitialMessage => InitialMessage != null;
        public ThreadMessageGet InitialMessage => Messages.FirstOrDefault(predicate: m => m.IsInitialMessage) ?? throw new Exception($"Thread \"{Id}\" does not have initial message");
    }
}