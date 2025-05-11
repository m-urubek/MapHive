namespace MapHive.Models.ViewModels
{
    using System.ComponentModel.DataAnnotations;
    using System.Data;

    public class SqlQueryViewModel
    {
        [Required(ErrorMessage = "Query is required")]
        public string Query { get; set; } = string.Empty;

        public DataTable? DataTable { get; set; }
        public bool HasResults { get; set; } = false;
        public int RowsAffected { get; set; } = 0;
        public string Message { get; set; } = string.Empty;
    }
}