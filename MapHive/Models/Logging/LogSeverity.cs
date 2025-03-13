namespace MapHive.Models
{
    public class LogSeverity
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }

        // Constants for common severity levels to avoid magic numbers
        public static readonly int Information = 1;
        public static readonly int Warning = 2;
        public static readonly int Error = 3;
        public static readonly int Critical = 4;
    }
}