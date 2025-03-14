using System.ComponentModel.DataAnnotations;

namespace MapHive.Models.ViewModels
{
    public class ThreadMessageViewModel
    {
        public int ThreadId { get; set; }
        
        [Required(ErrorMessage = "Message text is required")]
        [StringLength(1000, ErrorMessage = "Message text cannot be longer than 1000 characters")]
        public required string MessageText { get; set; }
        
        // For display purposes
        public string ThreadName { get; set; } = string.Empty;
    }
} 