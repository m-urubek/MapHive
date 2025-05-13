namespace MapHive.Models.Exceptions
{
    public class PublicErrorException : Exception
    {
        public PublicErrorException(string message)
            : base(message)
        {
        }

        public PublicErrorException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public PublicErrorException() : base()
        {
        }
    }
}
