namespace MapHive.Models.DataGrid
{
    public class DataGridRow
    {
        public Dictionary<string, DataGridCell> CellsByColumnNames { get; set; } = new Dictionary<string, DataGridCell>();
    }
}