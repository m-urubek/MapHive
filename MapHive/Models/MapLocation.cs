using System.ComponentModel.DataAnnotations;

namespace MapHive.Models
{
    public class MapLocation
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot be longer than 100 characters")]
        public required string Name { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(500, ErrorMessage = "Description cannot be longer than 500 characters")]
        public required string Description { get; set; }

        [Required(ErrorMessage = "Latitude is required")]
        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
        public double Latitude { get; set; }

        [Required(ErrorMessage = "Longitude is required")]
        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
        public double Longitude { get; set; }

        [StringLength(200, ErrorMessage = "Address cannot be longer than 200 characters")]
        public required string Address { get; set; }

        [DataType(DataType.Url)]
        [StringLength(255, ErrorMessage = "Website URL cannot be longer than 255 characters")]
        public required string Website { get; set; }

        [StringLength(50, ErrorMessage = "Phone number cannot be longer than 50 characters")]
        public required string PhoneNumber { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // User who created this place
        public int UserId { get; set; }

        // Flag for anonymous submission (hide author's username)
        public bool IsAnonymous { get; set; } = false;
    }
}