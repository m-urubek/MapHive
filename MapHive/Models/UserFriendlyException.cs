namespace MapHive.Models
{
    /// <summary>
    /// Exception type that is meant to be shown to the user as a friendly message
    /// instead of being logged as a server error
    /// </summary>
    public class UserFriendlyException : Exception
    {
        public UserFriendlyException(string message) : base(message)
        {
        }

        public UserFriendlyException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}