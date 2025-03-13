namespace MapHive.Models
{
    /// <summary>
    /// Base class for exceptions that display user-friendly messages
    /// </summary>
    public abstract class UserFriendlyExceptionBase : Exception
    {
        public enum MessageType
        {
            Blue,
            Orange,
            Red
        }

        public MessageType Type { get; protected set; }

        protected UserFriendlyExceptionBase(string message, MessageType type) : base(message)
        {
            this.Type = type;
        }

        protected UserFriendlyExceptionBase(string message, Exception innerException, MessageType type)
            : base(message, innerException)
        {
            this.Type = type;
        }
    }
}