using System.ComponentModel.DataAnnotations;

namespace MapHive.Models.RepositoryModels
{
    public class CategoryCreate
    {
        [Required(ErrorMessage = "Category name is required")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Category name must be between 2 and 50 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Description cannot exceed 200 characters")]
        public string Description { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "Icon name cannot exceed 20 characters")]
        public string Icon { get; set; } = string.Empty;
    }
}