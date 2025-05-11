namespace MapHive.Models.ViewModels
{
    using System.ComponentModel.DataAnnotations;
    using MapHive.Models.RepositoryModels;

    /// <summary>
    /// ViewModel for the Map/Add page, containing the create DTO and list of categories.
    /// </summary>
    public class AddLocationPageViewModel
    {
        /// <summary>
        /// The list of available categories for selection.
        /// </summary>
        public IEnumerable<CategoryGet> Categories { get; set; } = new List<CategoryGet>();

        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot be longer than 100 characters")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(500, ErrorMessage = "Description cannot be longer than 500 characters")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Latitude is required")]
        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
        public double? Latitude { get; set; }

        [Required(ErrorMessage = "Longitude is required")]
        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
        public double? Longitude { get; set; }

        [Required(ErrorMessage = "Address is required")]
        [StringLength(200, ErrorMessage = "Address cannot be longer than 200 characters")]
        public string? Address { get; set; }

        [Required(ErrorMessage = "Website is required")]
        [DataType(DataType.Url)]
        [StringLength(255, ErrorMessage = "Website URL cannot be longer than 255 characters")]
        public string? Website { get; set; }

        [Required(ErrorMessage = "PhoneNumber is required")]
        [StringLength(50, ErrorMessage = "Phone number cannot be longer than 50 characters")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "IsAnonymous is required")]
        public bool? IsAnonymous { get; set; } = false;

        [Required(ErrorMessage = "CategoryId is required")]
        public int? CategoryId { get; set; }
    }
}