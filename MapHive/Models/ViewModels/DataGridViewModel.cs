namespace MapHive.Models.ViewModels
{
    using MapHive.Models.RepositoryModels;

    public class DataGridViewModel
    {
        public required string Title { get; set; }
        public required List<string> ColumnNames { get; set; }
        public required List<DataGridColumnGet> Columns { get; set; }
        public required string TableName { get; set; }
    }
}