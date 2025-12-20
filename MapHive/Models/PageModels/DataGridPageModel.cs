namespace MapHive.Models.PageModels;

using MapHive.Models.Data.DataGrid;

public class DataGridPageModel
{
    public required string Title { get; set; }
    public required List<string>? ColumnNames { get; set; }
    public required List<DataGridColumn> Columns { get; set; }

    /// <summary>
    /// Whether to show the "Create New" button on the grid.
    /// Default is false - must be explicitly enabled for tables that support creation.
    /// </summary>
    public bool ShowCreateButton { get; set; } = false;
}