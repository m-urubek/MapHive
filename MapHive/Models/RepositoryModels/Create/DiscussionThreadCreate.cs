using System.ComponentModel.DataAnnotations;

namespace MapHive.Models.RepositoryModels
{
    public class DiscussionThreadCreate
    {
        [Required]
        public int LocationId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Thread name cannot exceed 100 characters")]
        public required string ThreadName { get; set; }

        public bool IsReviewThread { get; set; } = false;

        public int? ReviewId { get; set; }
    }
}