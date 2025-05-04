using System.ComponentModel.DataAnnotations;

namespace MapHive.Models.RepositoryModels
{
    public class ReviewThreadCreate
    {
        [Required]
        public int ReviewId { get; set; }

        [Required]
        public int LocationId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string ReviewTitle { get; set; } = string.Empty;
    }
}