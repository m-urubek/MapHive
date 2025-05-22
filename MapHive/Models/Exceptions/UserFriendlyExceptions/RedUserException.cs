namespace MapHive.Models.Exceptions;

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

    protected RedUserException(string message, MessageType type) : base(message, type)
    {
    }

    protected RedUserException(string message, Exception innerException, MessageType type) : base(message, innerException, type)
    {
    }

    public RedUserException() : base()
    {
    }
}