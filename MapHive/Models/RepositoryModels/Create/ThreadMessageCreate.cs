using System.ComponentModel.DataAnnotations;

namespace MapHive.Models.RepositoryModels
{
    public class ThreadMessageCreate
    {
        [Required]
        public int ThreadId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(1000, ErrorMessage = "Message cannot be longer than 1000 characters")]
        public required string MessageText { get; set; }

        public bool IsInitialMessage { get; set; } = false;
    }
}