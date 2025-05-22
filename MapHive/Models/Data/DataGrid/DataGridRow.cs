namespace MapHive.Models.Data.DataGrid;

public class DataGridRow
{
    public required int RowId { get; set; }
    public required Dictionary<string, string> ValuesByColumnNames { get; set; }
}
