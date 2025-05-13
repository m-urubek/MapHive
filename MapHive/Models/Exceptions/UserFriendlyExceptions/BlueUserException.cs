namespace MapHive.Models.Exceptions
{
    /// <summary>
    /// Exception that displays a blue information message to the user
    /// </summary>
    public class BlueUserException : UserFriendlyExceptionBase
    {
        public BlueUserException(string message)
            : base(message, MessageType.Blue)
        {
        }

        public BlueUserException(string message, Exception innerException)
            : base(message, innerException, MessageType.Blue)
        {
        }

        protected BlueUserException(string message, MessageType type) : base(message, type)
        {
        }

        protected BlueUserException(string message, Exception innerException, MessageType type) : base(message, innerException, type)
        {
        }

        public BlueUserException() : base()
        {
        }
    }
}