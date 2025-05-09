namespace MapHive.Models.RepositoryModels
{
    using System.ComponentModel.DataAnnotations;

    public class ThreadMessageGet
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

        public DateTime CreatedAt { get; set; }

        public DateTime? DeletedAt { get; set; }

        // Populated by repository
        public string AuthorName { get; set; } = string.Empty;

        // Populated by repository on delete
        public string? DeletedByUsername { get; set; }
    }
}