namespace MapHive.Models.BusinessModels
{
    public class ColumnInfo
    {
        public string TableName { get; set; } = string.Empty;
        public string ColumnName { get; set; } = string.Empty;
        public bool IsForeignKey { get; set; } = false;
        public string? ForeignTable { get; set; }
        public string? ForeignColumn { get; set; }
    }
}