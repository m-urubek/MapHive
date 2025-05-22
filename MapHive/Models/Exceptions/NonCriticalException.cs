namespace MapHive.Models.Exceptions;

public class NonCriticalException : Exception
{
    public NonCriticalException(string message)
        : base(message)
    {
    }

    public NonCriticalException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public NonCriticalException() : base()
    {
    }
}
