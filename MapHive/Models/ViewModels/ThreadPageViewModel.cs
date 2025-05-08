namespace MapHive.Models.ViewModels
{
    /// <summary>
    /// ViewModel for the Discussion Thread page, including thread details and the new message form.
    /// </summary>
    public class ThreadPageViewModel
    {
        /// <summary>
        /// The discussion thread details.
        /// </summary>
        public required ThreadDetailsViewModel ThreadDetails { get; set; }

        /// <summary>
        /// The model for adding a new thread message.
        /// </summary>
        public required ThreadMessageViewModel NewMessage { get; set; }
    }
}