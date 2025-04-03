using System.ComponentModel.DataAnnotations;

namespace MapHive.Models
{
    public class DiscussionThread
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

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties (for reference, not used in SQLite directly)
        public MapLocation? Location { get; set; }
        public Review? Review { get; set; }
        public List<ThreadMessage> Messages { get; set; } = new List<ThreadMessage>();

        // This property will be populated by the repository
        public string AuthorName { get; set; } = string.Empty;

        // Property to check if initial message exists
        public bool HasInitialMessage => this.Messages.Any(m => m.IsInitialMessage);

        // Property to get the initial message
        public ThreadMessage? InitialMessage => this.Messages.FirstOrDefault(m => m.IsInitialMessage);
    }

    public class DiscussionReply
    {
        public int ReplyId { get; set; }
        public int ThreadId { get; set; }
        public required string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public int UserId { get; set; }
        public required string Username { get; set; }
    }
}