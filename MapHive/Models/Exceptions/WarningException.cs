namespace MapHive.Models.Exceptions;

public class PublicWarningException : Exception
{
    public PublicWarningException(string message)
        : base(message)
    {
    }

    public PublicWarningException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public PublicWarningException() : base()
    {
    }
}
