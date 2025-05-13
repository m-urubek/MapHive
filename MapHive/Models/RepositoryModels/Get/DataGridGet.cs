namespace MapHive.Models.RepositoryModels
{
    public class DataGridGet
    {
        public required List<DataGridColumnGet> Columns { get; set; }
        public required List<DataGridRowGet> Items { get; set; }
        public bool HasItems => Items.Count > 0;
        public required int CurrentPage { get; set; }
        public required int PageSize { get; set; }
        public required int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(a: TotalCount / (double)PageSize);
        public string? SearchTerm { get; set; }
        public string? SearchColumn { get; set; }
        public required string SortField { get; set; }
        public required string SortDirection { get; set; }
        public required string TableName { get; set; }
    }
}