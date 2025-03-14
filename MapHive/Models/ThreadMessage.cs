using System.ComponentModel.DataAnnotations;

namespace MapHive.Models
{
    public class ThreadMessage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ThreadId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(1000, ErrorMessage = "Message text cannot be longer than 1000 characters")]
        public required string MessageText { get; set; }

        public bool IsInitialMessage { get; set; } = false;

        public bool IsDeleted { get; set; } = false;
        
        public int? DeletedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties (for reference, not used in SQLite directly)
        public DiscussionThread? Thread { get; set; }
        
        // This property will be populated by the repository
        public string AuthorName { get; set; } = string.Empty;
        
        // This property will be populated by the repository when a message is deleted
        public string? DeletedByUsername { get; set; }
    }
} 