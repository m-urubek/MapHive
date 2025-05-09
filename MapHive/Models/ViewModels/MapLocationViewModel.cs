namespace MapHive.Models.ViewModels
{
    using MapHive.Models.RepositoryModels;

    public class MapLocationViewModel
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public required string Address { get; set; }
        public required string Website { get; set; }
        public required string PhoneNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int UserId { get; set; }
        public bool IsAnonymous { get; set; }
        public int? CategoryId { get; set; }
        public CategoryGet? Category { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public IEnumerable<ReviewGet> Reviews { get; set; } = new List<ReviewGet>();
        public IEnumerable<DiscussionThreadGet> Discussions { get; set; } = new List<DiscussionThreadGet>();
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public int RegularDiscussionCount { get; set; }
    }
}