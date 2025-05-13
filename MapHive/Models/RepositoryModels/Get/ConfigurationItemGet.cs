namespace MapHive.Models.RepositoryModels
{
    public class ConfigurationItemGet
    {
        public required int Id { get; set; }
        public required string Key { get; set; } = string.Empty;
        public required object Value { get; set; } = default!;
        public string? Description { get; set; }
    }
}