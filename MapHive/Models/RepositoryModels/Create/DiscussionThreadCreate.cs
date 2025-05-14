namespace MapHive.Models.RepositoryModels
{
    using System.ComponentModel.DataAnnotations;

    public class DiscussionThreadCreate
    {
        [Required]
        public required int LocationId { get; set; }

        [Required]
        public required int AccountId { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Thread name cannot be longer than 100 characters")]
        public required string ThreadName { get; set; }

        public required bool IsReviewThread { get; set; }

        public int? ReviewId { get; set; }

        public required DateTime CreatedAt { get; set; }

        public required bool IsAnonymous { get; set; }
    }
}
