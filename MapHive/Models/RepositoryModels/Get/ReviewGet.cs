namespace MapHive.Models.RepositoryModels
{
    using System.ComponentModel.DataAnnotations;

    public class ReviewGet
    {
        [Key]
        public required int Id { get; set; }

        [Required]
        public required int LocationId { get; set; }

        [Required]
        public required int UserId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public required int Rating { get; set; }

        [Required]
        [StringLength(500, ErrorMessage = "Review text cannot be longer than 500 characters")]
        public required string ReviewText { get; set; }

        public required bool IsAnonymous { get; set; }

        public required DateTime CreatedAt { get; set; }

        public required DateTime UpdatedAt { get; set; }

        public required string AuthorName { get; set; } = string.Empty;
    }
}