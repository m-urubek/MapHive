namespace MapHive.Models.Data.DbTableModels;

public class ConfigurationAtomic
{
    public required int Id { get; set; }
    public required string Key { get; set; }
    public required object Value { get; set; }
    public string? Description { get; set; }
}
