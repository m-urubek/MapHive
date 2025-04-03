using System.ComponentModel.DataAnnotations;
using System.Data;

namespace MapHive.Models
{
    public class UsersViewModel
    {
        public IEnumerable<User> Users { get; set; } = new List<User>();
        public string SearchTerm { get; set; } = string.Empty;
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalCount { get; set; } = 0;
        public int TotalPages { get; set; } = 1;
    }

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