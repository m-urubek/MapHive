namespace MapHive.Models.ViewModels
{
    using System.ComponentModel.DataAnnotations;
    using MapHive.Models.Enums;

    public class BanViewModel
    {
        [Required]
        public required BanType BanType { get; set; }

        [StringLength(500)]
        public string? Reason { get; set; }

        public required bool IsPermanent { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Duration must be at least 1 day")]
        public int? BanDurationDays { get; set; }

        [Required]
        public required int AccountId { get; set; }
    }
}
