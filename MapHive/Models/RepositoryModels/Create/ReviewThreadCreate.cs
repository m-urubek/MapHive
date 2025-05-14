namespace MapHive.Models.RepositoryModels
{
    using System.ComponentModel.DataAnnotations;

    public class ReviewThreadCreate
    {
        [Required]
        public required int ReviewId { get; set; }

        [Required]
        public required int LocationId { get; set; }

        [Required]
        public required int AccountId { get; set; }

        [Required]
        public required string Username { get; set; }

        [Required]
        public required string ReviewTitle { get; set; }

        [Required]
        public required bool IsAnonymous { get; set; }
    }
}
