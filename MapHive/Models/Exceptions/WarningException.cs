namespace MapHive.Models.Exceptions
{
    public class WarningException : Exception
    {
        public WarningException(string message)
            : base(message)
        {
        }

        public WarningException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public WarningException() : base()
        {
        }
    }
}
