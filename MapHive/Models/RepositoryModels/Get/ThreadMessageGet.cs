namespace MapHive.Models.RepositoryModels
{
    using System.ComponentModel.DataAnnotations;

    public class ThreadMessageGet
    {
        [Key]
        public required int Id { get; set; }

        [Required]
        public required int ThreadId { get; set; }

        [Required]
        public required int UserId { get; set; }

        [Required]
        [StringLength(1000, ErrorMessage = "Message text cannot be longer than 1000 characters")]
        public required string MessageText { get; set; }

        public required bool IsInitialMessage { get; set; } = false;

        public required bool IsDeleted { get; set; } = false;

        public int? DeletedByUserId { get; set; }

        public required DateTime CreatedAt { get; set; }

        public DateTime? DeletedAt { get; set; }

        // Populated by repository
        public required string AuthorName { get; set; } = string.Empty;

        // Populated by repository on delete
        public string? DeletedByUsername { get; set; }
    }
}