namespace MapHive.Models.ViewModels
{
    using System.ComponentModel.DataAnnotations;

    public class DiscussionThreadViewModel
    {
        public int LocationId { get; set; }

        [Required(ErrorMessage = "Thread name is required")]
        [StringLength(100, ErrorMessage = "Thread name cannot be longer than 100 characters")]
        public required string ThreadName { get; set; }

        [Required(ErrorMessage = "Initial message is required")]
        [StringLength(1000, ErrorMessage = "Message text cannot be longer than 1000 characters")]
        public required string InitialMessage { get; set; }

        // For display purposes
        public string LocationName { get; set; } = string.Empty;
    }
}