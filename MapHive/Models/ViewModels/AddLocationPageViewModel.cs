namespace MapHive.Models.ViewModels
{
    using MapHive.Models.RepositoryModels;

    /// <summary>
    /// ViewModel for the Map/Add page, containing the create DTO and list of categories.
    /// </summary>
    public class AddLocationPageViewModel
    {
        /// <summary>
        /// The DTO for creating a new map location.
        /// </summary>
        public required MapLocationCreate CreateModel { get; set; }

        /// <summary>
        /// The list of available categories for selection.
        /// </summary>
        public IEnumerable<CategoryGet> Categories { get; set; } = new List<CategoryGet>();
    }
}