namespace MapHive.Models.DataGrid
{
    public class DataGridColumn
    {
        public string DisplayName { get; set; } = string.Empty;
        public string InternalName { get; set; } = string.Empty;
        public string Flex { get; set; } = "1 1 auto"; //TODO: Is this needed?
        public int Index { get; set; }
        public bool IsLastColumn { get; set; }
    }
}