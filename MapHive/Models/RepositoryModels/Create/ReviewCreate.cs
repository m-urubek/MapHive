namespace MapHive.Models.RepositoryModels
{
    using System.ComponentModel.DataAnnotations;

    public class ReviewCreate
    {
        [Required]
        public int LocationId { get; set; }

        [Required]
        public int AccountId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        [Required]
        [StringLength(500, ErrorMessage = "Review text cannot be longer than 500 characters")]
        public required string ReviewText { get; set; }

        public bool IsAnonymous { get; set; } = false;
    }
}