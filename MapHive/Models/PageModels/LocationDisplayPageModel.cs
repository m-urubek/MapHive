namespace MapHive.Models.PageModels;

using MapHive.Models.Data.DbTableModels;

public class LocationDisplayPageModel
{
    public required string Name { get; set; }
    public required string? Description { get; set; }
    public required double Latitude { get; set; }
    public required double Longitude { get; set; }
    public required string? Address { get; set; }
    public required string? Website { get; set; }
    public required string? PhoneNumber { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required DateTime UpdatedAt { get; set; }
    public required bool IsAnonymous { get; set; }
    public required string CategoryName { get; set; }
    public required bool CanEdit { get; set; }
    public required bool HasReviewed { get; set; }
    public required string OwnerUsername { get; set; } //todo use join properly
    public required int? OwnerId { get; set; }
    public required int RegularDiscussionCount { get; set; }
    public required List<ThreadInitialMessageDbModel>? Threads { get; set; }
    public required List<ReviewDisplayPageModel>? Reviews { get; set; }

    public double? AverageRating => Reviews?.Select(r => r.Rating).Average();
    public int ReviewCount => Reviews?.Count ?? 0;
}
