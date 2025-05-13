namespace MapHive.Models.RepositoryModels
{
    using System.ComponentModel.DataAnnotations;

    public class ConfigurationItem
    {
        public int Id { get; set; }

        [Required]
        [Key]
        public string Key { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}