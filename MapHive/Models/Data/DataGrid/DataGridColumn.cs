namespace MapHive.Models.Data.DataGrid;

public class DataGridColumn
{
    public required string TableName { get; set; }
    public required string DisplayName { get; set; }
    public required string InternalName { get; set; }
    public required int Index { get; set; }
    public required string Flex { get; set; }
    public required bool IsLastColumn { get; set; }
    public string? LinkedForeignKey { get; set; }
    public string? JoinAlias { get; set; }
}
