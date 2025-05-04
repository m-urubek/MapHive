using MapHive.Models.RepositoryModels;

namespace MapHive.Models.ViewModels
{
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
        public bool HasInitialMessage => this.InitialMessage != null;
        public ThreadMessageGet? InitialMessage => this.Messages.FirstOrDefault(m => m.IsInitialMessage);
    }
}