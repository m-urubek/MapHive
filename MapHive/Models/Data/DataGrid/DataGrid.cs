namespace MapHive.Models.Data.DataGrid;

public class DataGrid
{
    public required List<DataGridColumn> Columns { get; set; }
    public required List<DataGridRow> Items { get; set; }
    public required int CurrentPage { get; set; }
    public required int PageSize { get; set; }
    public required int TotalCount { get; set; }
    public required string? SearchTerm { get; set; }
    public required string? SearchColumn { get; set; }
    public required string SortField { get; set; }
    public required bool Ascending { get; set; }
    public required string TableName { get; set; }

    public int TotalPages => (int)Math.Ceiling(a: TotalCount / (double)PageSize);
    public bool HasItems => Items.Count > 0;
}
