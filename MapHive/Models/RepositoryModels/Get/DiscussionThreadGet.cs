using System.ComponentModel.DataAnnotations;

namespace MapHive.Models.RepositoryModels
{
    public class DiscussionThreadGet
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int LocationId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Thread name cannot be longer than 100 characters")]
        public required string ThreadName { get; set; }

        public bool IsReviewThread { get; set; } = false;

        public int? ReviewId { get; set; }

        public DateTime CreatedAt { get; set; }

        // This property will be populated by the repository
        public string AuthorName { get; set; } = string.Empty;

        // Messages in this thread
        public List<ThreadMessageGet> Messages { get; set; } = new List<ThreadMessageGet>();

        // Helpers for initial message
        public bool HasInitialMessage => this.Messages.Any(m => m.IsInitialMessage);
        public ThreadMessageGet? InitialMessage => this.Messages.FirstOrDefault(m => m.IsInitialMessage);
    }
}