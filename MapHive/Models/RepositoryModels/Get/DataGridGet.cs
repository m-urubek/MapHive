namespace MapHive.Models.RepositoryModels
{
    public class DataGridGet
    {
        public List<DataGridColumnGet> Columns { get; set; } = new List<DataGridColumnGet>();
        public List<DataGridRowGet> Items { get; set; } = new List<DataGridRowGet>();
        public bool HasItems => Items.Count > 0;
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalCount { get; set; } = 0;
        public int TotalPages => (int)Math.Ceiling(a: TotalCount / (double)PageSize);
        public string SearchTerm { get; set; } = string.Empty;
        public string SearchColumn { get; set; } = string.Empty;
        public string SortField { get; set; } = string.Empty;
        public string SortDirection { get; set; } = "asc";
        public string TableName { get; set; } = string.Empty;
    }
}