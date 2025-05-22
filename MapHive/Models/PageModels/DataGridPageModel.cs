namespace MapHive.Models.PageModels;

using MapHive.Models.Data.DataGrid;

public class DataGridPageModel
{
    public required string Title { get; set; }
    public required List<string>? ColumnNames { get; set; }
    public required List<DataGridColumn> Columns { get; set; }
}