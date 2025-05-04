namespace MapHive.Models.RepositoryModels
{
    public class DataGridRowGet
    {
        /// <summary>
        /// The unique identifier for this row
        /// </summary>
        public int RowId { get; set; }

        /// <summary>
        /// Dictionary of cells by column names
        /// </summary>
        public Dictionary<string, DataGridCellGet> CellsByColumnNames { get; set; } = new Dictionary<string, DataGridCellGet>();
    }
}