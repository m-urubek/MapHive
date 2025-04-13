namespace MapHive.Models.DataGrid
{
    public class DataGrid
    {
        public List<DataGridColumn> Columns { get; set; } = new List<DataGridColumn>();
        public List<DataGridRow> Items { get; set; } = new List<DataGridRow>();
        public bool HasItems => this.Items.Count > 0;
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalCount { get; set; } = 0;
        public int TotalPages => (int)Math.Ceiling(this.TotalCount / (double)this.PageSize);
        public string SearchTerm { get; set; } = string.Empty;
        public string SearchColumn { get; set; } = string.Empty;
        public string SortField { get; set; } = string.Empty;
        public string SortDirection { get; set; } = "asc";
        public string TableName { get; set; } = string.Empty;
    }
}