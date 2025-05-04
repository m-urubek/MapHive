namespace MapHive.Models.RepositoryModels
{
    public class DataGridColumnGet
    {
        public string DisplayName { get; set; } = string.Empty;
        public string InternalName { get; set; } = string.Empty;
        public int Index { get; set; }
        public string Flex { get; set; } = string.Empty;
        public bool IsLastColumn { get; set; } = false;
    }
}