namespace MapHive.Models.RepositoryModels
{
    using System.ComponentModel.DataAnnotations;

    public class MapLocationUpdate
    {
        [Required]
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
        public string Address { get; set; } = string.Empty;

        [DataType(DataType.Url)]
        [StringLength(255, ErrorMessage = "Website URL cannot be longer than 255 characters")]
        public string Website { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Phone number cannot be longer than 50 characters")]
        public string PhoneNumber { get; set; } = string.Empty;

        public bool IsAnonymous { get; set; } = false;

        public int CategoryId { get; set; }
    }
}