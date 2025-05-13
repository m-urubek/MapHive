namespace MapHive.Models.RepositoryModels
{
    public class DataGridRowGet
    {
        /// <summary>
        /// The unique identifier for this row
        /// </summary>
        public required int RowId { get; set; }

        /// <summary>
        /// Dictionary of cells by column names
        /// </summary>
        public required Dictionary<string, DataGridCellGet> CellsByColumnNames { get; set; }
    }
}