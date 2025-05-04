namespace MapHive.Models.RepositoryModels
{
    public class ConfigurationItemGet
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public object Value { get; set; } = default!;
        public string? Description { get; set; }
    }
}