using System.ComponentModel.DataAnnotations;

namespace MapHive.Models
{
    public class BlacklistedAddress
    {
        public int Id { get; set; }

        public string? IpAddress { get; set; }

        public string? MacAddress { get; set; }

        [Required]
        public string Reason { get; set; } = string.Empty;

        public DateTime BlacklistedDate { get; set; }
    }
}