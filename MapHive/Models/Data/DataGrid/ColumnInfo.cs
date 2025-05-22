namespace MapHive.Models.Data.DataGrid;

public class ColumnInfo
{
    public required string TableName { get; set; }
    public required string ColumnName { get; set; }
    public required bool IsForeignKey { get; set; }
    public required string? ForeignTable { get; set; }
    public required string? ForeignColumn { get; set; }
}
