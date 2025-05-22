namespace MapHive.Models.Exceptions;

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

    protected OrangeUserException(string message, MessageType type) : base(message, type)
    {
    }

    protected OrangeUserException(string message, Exception innerException, MessageType type) : base(message, innerException, type)
    {
    }

    public OrangeUserException() : base()
    {
    }
}