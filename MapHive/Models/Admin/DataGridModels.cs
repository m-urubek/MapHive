using Microsoft.AspNetCore.Mvc;

namespace MapHive.Models
{
    public class DataGridViewModel
    {
        public List<DataGridColumn> Columns { get; set; } = new List<DataGridColumn>();
        public List<DataGridRow> Items { get; set; } = new List<DataGridRow>();
        public bool HasItems => Items.Count > 0;
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalCount { get; set; } = 0;
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public string SearchTerm { get; set; } = string.Empty;
        public string SortField { get; set; } = string.Empty;
        public string SortDirection { get; set; } = "asc";
        public string ControllerName { get; set; } = string.Empty;
        public string ActionName { get; set; } = string.Empty;
        public string GridId { get; set; } = string.Empty;

        public string GetPageUrl(int page)
        {
            // Build the URL for pagination
            return $"/{ControllerName}/{ActionName}?page={page}&searchTerm={SearchTerm}&sortField={SortField}&sortDirection={SortDirection}";
        }
        
        // Method to fetch data via AJAX
        public string GetDataUrl()
        {
            return $"/{ControllerName}/GetGridData?gridId={GridId}&page={CurrentPage}&searchTerm={SearchTerm}&sortField={SortField}&sortDirection={SortDirection}";
        }
    }

    public class DataGridColumn
    {
        public string Title { get; set; } = string.Empty;
        public string Field { get; set; } = string.Empty;
        public string Flex { get; set; } = "1 1 auto";
        public int Index { get; set; }
        public bool IsLastColumn { get; set; }
    }

    public class DataGridRow
    {
        public List<DataGridCell> Cells { get; set; } = new List<DataGridCell>();
    }

    public class DataGridCell
    {
        public string Content { get; set; } = string.Empty;
        public string Flex { get; set; } = "1 1 auto";
    }
} 