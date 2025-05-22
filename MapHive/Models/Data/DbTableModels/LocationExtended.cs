namespace MapHive.Models.Data.DbTableModels;

public class LocationExtended
{
    public required int Id { get; set; }
    public required string Name { get; set; }
    public required string? Description { get; set; }
    public required double Latitude { get; set; }
    public required double Longitude { get; set; }
    public required string? Address { get; set; }
    public required string? Website { get; set; }
    public required string? PhoneNumber { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required DateTime UpdatedAt { get; set; }
    public required int OwnerId { get; set; }
    public required string OwnerUsername { get; set; }
    public required bool IsAnonymous { get; set; } = false;
    public required int CategoryId { get; set; }
    public required string CategoryName { get; set; }
}
