namespace MapHive.Models.ViewModels
{
    using System.ComponentModel.DataAnnotations;

    public class ReviewViewModel
    {
        public int LocationId { get; set; }

        [Required(ErrorMessage = "Rating is required")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "Review text is required")]
        [StringLength(500, ErrorMessage = "Review text cannot be longer than 500 characters")]
        public required string ReviewText { get; set; }

        public bool IsAnonymous { get; set; } = false;

        // For display purposes
        public string LocationName { get; set; } = string.Empty;
    }
}