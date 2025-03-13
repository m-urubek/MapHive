namespace MapHive.Models
{
    /// <summary>
    /// Exception that displays an orange warning message to the user
    /// </summary>
    public class OrangeUserException : UserFriendlyExceptionBase
    {
        public OrangeUserException(string message)
            : base(message, MessageType.Orange)
        {
        }

        public OrangeUserException(string message, Exception innerException)
            : base(message, innerException, MessageType.Orange)
        {
        }
    }
}