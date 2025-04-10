using System.Collections.Generic;

namespace MapHive.Models.DataGrid
{
    public class DataGridRow
    {
        /// <summary>
        /// The unique identifier for this row, used for redirection to detail page
        /// </summary>
        public int RowId { get; set; }
        
        /// <summary>
        /// Dictionary of cells by column names
        /// </summary>
        public Dictionary<string, DataGridCell> CellsByColumnNames { get; set; } = new Dictionary<string, DataGridCell>();
    }
}