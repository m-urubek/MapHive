namespace MapHive.Models
{
    /// <summary>
    /// Exception that displays a red error message to the user
    /// </summary>
    public class RedUserException : UserFriendlyExceptionBase
    {
        public RedUserException(string message)
            : base(message, MessageType.Red)
        {
        }

        public RedUserException(string message, Exception innerException)
            : base(message, innerException, MessageType.Red)
        {
        }
    }
}