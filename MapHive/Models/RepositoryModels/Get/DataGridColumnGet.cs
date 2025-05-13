namespace MapHive.Models.RepositoryModels
{
    public class DataGridColumnGet
    {
        public required string DisplayName { get; set; }
        public required string InternalName { get; set; }
        public required int Index { get; set; }
        public required string Flex { get; set; }
        public required bool IsLastColumn { get; set; }
    }
}