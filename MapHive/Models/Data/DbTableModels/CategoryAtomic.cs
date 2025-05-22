namespace MapHive.Models.Data.DbTableModels;

public class CategoryAtomic
{
    public required int Id { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public string? Icon { get; set; }
}
