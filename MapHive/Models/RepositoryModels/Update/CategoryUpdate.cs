namespace MapHive.Models.RepositoryModels
{
    using System.ComponentModel.DataAnnotations;

    public class CategoryUpdate
    {
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage = "CategoryName name is required")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "CategoryName name must be between 2 and 50 characters")]
        public required string Name { get; set; }

        [StringLength(200, ErrorMessage = "Description cannot exceed 200 characters")]
        public string? Description { get; set; }

        [StringLength(20, ErrorMessage = "Icon name cannot exceed 20 characters")]
        public string? Icon { get; set; }
    }
}