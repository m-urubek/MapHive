namespace MapHive.Models
{
    public class AppSettings
    {
        public bool DevelopmentMode { get; set; }

        // Default constructor with sensible defaults
        public AppSettings()
        {
            this.DevelopmentMode = false;
        }
    }
}