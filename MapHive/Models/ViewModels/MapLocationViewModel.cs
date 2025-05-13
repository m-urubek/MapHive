namespace MapHive.Models.ViewModels
{
    using MapHive.Models.RepositoryModels;

    public class MapLocationViewModel
    {
        public required MapLocationGet MapLocationGet { get; set; }
        /// <summary> If null, user not logged in </summary>
        public bool? HasReviewed { get; set; }
        /// <summary> If null, is anonymous </summary>
        public string? AuthorUsername { get; set; }
        /// <summary> If null, no reviews yet </summary>
        public required int ReviewCount { get; set; }
        public required int RegularDiscussionCount { get; set; }
        public List<DiscussionThreadGet>? Discussions { get; set; }
        public List<ReviewGet>? Reviews { get; set; }
        public double? AverageRating => Reviews?.Select(r => r.Rating).Average();
    }
}