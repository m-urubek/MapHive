namespace MapHive.Models
{
    using System.ComponentModel.DataAnnotations;
    using MapHive.Models.Enums;

    public class BanViewModel
    {
        [Required]
        public BanType BanType { get; set; }

        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;

        public bool IsPermanent { get; set; } = true;

        [Range(1, int.MaxValue, ErrorMessage = "Duration must be at least 1 day")]
        public int? BanDurationDays { get; set; }
    }
}