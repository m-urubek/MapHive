namespace MapHive.Models.ViewModels
{
    using MapHive.Models.RepositoryModels;

    /// <summary>
    /// ViewModel for the Map/Edit page, containing the update DTO and list of categories.
    /// </summary>
    public class EditLocationPageViewModel
    {
        /// <summary>
        /// The DTO for updating a map location.
        /// </summary>
        public required MapLocationUpdate UpdateModel { get; set; }

        /// <summary>
        /// The list of available categories for selection.
        /// </summary>
        public IEnumerable<CategoryGet> Categories { get; set; } = new List<CategoryGet>();
    }
}