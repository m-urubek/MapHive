namespace MapHive.Models.RepositoryModels
{
    using System.ComponentModel.DataAnnotations;

    public class DiscussionThreadGet //TODO make view model
    {
        [Key]
        public required int Id { get; set; }

        [Required]
        public required int LocationId { get; set; }

        [Required]
        public required int AccountId { get; set; } //should get to FE when anonymous

        [Required]
        [StringLength(100, ErrorMessage = "Thread name cannot be longer than 100 characters")]
        public required string ThreadName { get; set; }

        public required bool IsReviewThread { get; set; }

        public int? ReviewId { get; set; }

        public required DateTime CreatedAt { get; set; }

        // This property will be populated by the repository
        public required string AuthorUsername { get; set; }

        public required bool IsAnonymous { get; set; }

        // Messages in this thread
        public required List<ThreadMessageGet> Messages { get; set; }

        public ThreadMessageGet InitialMessage => Messages.FirstOrDefault(predicate: m => m.IsInitialMessage) ?? throw new Exception($"Thread \"{Id}\" does not have initial message");
    }
}
