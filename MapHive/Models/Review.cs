using System.ComponentModel.DataAnnotations;

namespace MapHive.Models
{
    public class Review
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int LocationId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        [Required]
        [StringLength(500, ErrorMessage = "Review text cannot be longer than 500 characters")]
        public required string ReviewText { get; set; }

        public bool IsAnonymous { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties (for reference, not used in SQLite directly)
        public MapLocation? Location { get; set; }
        
        // This property will be populated by the repository
        public string AuthorName { get; set; } = string.Empty;
    }
} 