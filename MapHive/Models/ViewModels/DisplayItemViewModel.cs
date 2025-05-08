namespace MapHive.Models.ViewModels
{
    public class DisplayItemViewModel
    {
        // The name of the table
        public string TableName { get; set; } = string.Empty;

        // The ID of the item
        public int ItemId { get; set; }

        // The data for the item (column display name -> value)
        public Dictionary<string, string> Data { get; set; } = new Dictionary<string, string>();

        // Indicates whether this is a Users table entry
        public bool IsUsersTable { get; set; } = false;

        // Username when displaying a Users table entry
        public string? Username { get; set; }
    }
}