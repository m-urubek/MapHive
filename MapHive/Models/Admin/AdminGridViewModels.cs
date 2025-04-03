using System.Collections.Generic;

namespace MapHive.Models
{
    // Base class for all admin grid view models
    public abstract class AdminGridViewModel
    {
        public DataGridViewModel Grid { get; set; } = new DataGridViewModel();

        // Method to be implemented by derived classes to define columns
        protected abstract void DefineColumns();
        
        // Method to be implemented by derived classes to populate data
        protected abstract void PopulateRows();
        
        // Initialize the grid with columns and data
        public void InitializeGrid()
        {
            DefineColumns();
            PopulateRows();
        }
    }
    
    // Updated BansGridViewModel to inherit from AdminGridViewModel
    public class BansGridViewModel : AdminGridViewModel
    {
        public IEnumerable<UserBan> Bans { get; set; } = Enumerable.Empty<UserBan>();
        public string SearchTerm { get; set; } = string.Empty;
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalCount { get; set; } = 0;
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        
        protected override void DefineColumns()
        {
            Grid.GridId = "banGrid";
            Grid.ControllerName = "Admin";
            Grid.ActionName = "Bans";
            Grid.CurrentPage = CurrentPage;
            Grid.PageSize = PageSize;
            Grid.TotalCount = TotalCount;
            Grid.SearchTerm = SearchTerm;
            
            // Define columns
            Grid.Columns.Add(new DataGridColumn { Title = "ID", Field = "Id", Flex = "0 0 60px", Index = 0 });
            Grid.Columns.Add(new DataGridColumn { Title = "Ban Type", Field = "BanType", Flex = "1 1 150px", Index = 1 });
            Grid.Columns.Add(new DataGridColumn { Title = "Target", Field = "", Flex = "1 1 150px", Index = 2 });
            Grid.Columns.Add(new DataGridColumn { Title = "Banned At", Field = "BannedAt", Flex = "1 1 150px", Index = 3 });
            Grid.Columns.Add(new DataGridColumn { Title = "Expires At", Field = "ExpiresAt", Flex = "1 1 150px", Index = 4 });
            Grid.Columns.Add(new DataGridColumn { Title = "Status", Field = "Status", Flex = "0 0 100px", Index = 5, IsLastColumn = true });
        }
        
        protected override void PopulateRows()
        {
            if (Bans == null || !Bans.Any())
                return;
                
            foreach (var ban in Bans)
            {
                string target = ban.BanType == BanType.Account 
                    ? (ban.Properties.TryGetValue("BannedUsername", out string? username) ? username : "User ID: " + ban.UserId)
                    : "IP: " + ban.IpAddress;
                    
                var row = new DataGridRow();
                
                // ID Cell
                row.Cells.Add(new DataGridCell 
                { 
                    Content = $"<a href=\"/Admin/BanDetails/{ban.Id}\" class=\"grid-link\">{ban.Id}</a>", 
                    Flex = "0 0 60px" 
                });
                
                // Ban Type Cell
                row.Cells.Add(new DataGridCell 
                { 
                    Content = ban.BanType == BanType.Account ? "Account Ban" : "IP Ban", 
                    Flex = "1 1 150px" 
                });
                
                // Target Cell
                row.Cells.Add(new DataGridCell 
                { 
                    Content = target, 
                    Flex = "1 1 150px" 
                });
                
                // Banned At Cell
                row.Cells.Add(new DataGridCell 
                { 
                    Content = ban.BannedAt.ToString("g"), 
                    Flex = "1 1 150px" 
                });
                
                // Expires At Cell
                row.Cells.Add(new DataGridCell 
                { 
                    Content = ban.ExpiresAt.HasValue ? ban.ExpiresAt.Value.ToString("g") : "Never (Permanent)", 
                    Flex = "1 1 150px" 
                });
                
                // Status Cell
                row.Cells.Add(new DataGridCell 
                { 
                    Content = $"<span class=\"status-badge {(ban.IsActive ? "badge-active" : "badge-expired")}\">{(ban.IsActive ? "Active" : "Expired")}</span>", 
                    Flex = "0 0 100px" 
                });
                
                Grid.Items.Add(row);
            }
        }
    }
    
    // Updated UsersGridViewModel to inherit from AdminGridViewModel
    public class UsersGridViewModel : AdminGridViewModel
    {
        public IEnumerable<User> Users { get; set; } = Enumerable.Empty<User>();
        public string SearchTerm { get; set; } = string.Empty;
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalCount { get; set; } = 0;
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        
        protected override void DefineColumns()
        {
            Grid.GridId = "userGrid";
            Grid.ControllerName = "Admin";
            Grid.ActionName = "Users";
            Grid.CurrentPage = CurrentPage;
            Grid.PageSize = PageSize;
            Grid.TotalCount = TotalCount;
            Grid.SearchTerm = SearchTerm;
            
            // Define columns
            Grid.Columns.Add(new DataGridColumn { Title = "ID", Field = "Id", Flex = "0 0 60px", Index = 0 });
            Grid.Columns.Add(new DataGridColumn { Title = "Username", Field = "Username", Flex = "1 1 200px", Index = 1 });
            Grid.Columns.Add(new DataGridColumn { Title = "Registration Date", Field = "RegistrationDate", Flex = "1 1 150px", Index = 2 });
            Grid.Columns.Add(new DataGridColumn { Title = "Registration IP", Field = "IpAddress", Flex = "1 1 150px", Index = 3 });
            Grid.Columns.Add(new DataGridColumn { Title = "User Tier", Field = "Tier", Flex = "0 0 100px", Index = 4, IsLastColumn = true });
        }
        
        protected override void PopulateRows()
        {
            if (Users == null || !Users.Any())
                return;
                
            foreach (var user in Users)
            {
                string registrationIp = user.IpAddressHistory?.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "N/A";
                    
                var row = new DataGridRow();
                
                // ID Cell
                row.Cells.Add(new DataGridCell 
                { 
                    Content = user.Id.ToString(), 
                    Flex = "0 0 60px" 
                });
                
                // Username Cell
                row.Cells.Add(new DataGridCell 
                { 
                    Content = $"<a href=\"/Account/PublicProfile?username={user.Username}\" class=\"grid-link\">{user.Username}</a>", 
                    Flex = "1 1 200px" 
                });
                
                // Registration Date Cell
                row.Cells.Add(new DataGridCell 
                { 
                    Content = user.RegistrationDate.ToString("g"), 
                    Flex = "1 1 150px" 
                });
                
                // Registration IP Cell
                row.Cells.Add(new DataGridCell 
                { 
                    Content = registrationIp, 
                    Flex = "1 1 150px" 
                });
                
                // User Tier Cell
                string badgeClass = user.Tier == UserTier.Admin ? "badge-admin" : user.Tier == UserTier.Trusted ? "badge-trusted" : "badge-normal";
                row.Cells.Add(new DataGridCell 
                { 
                    Content = $"<span class=\"status-badge {badgeClass}\">{user.Tier}</span>", 
                    Flex = "0 0 100px" 
                });
                
                Grid.Items.Add(row);
            }
        }
    }
} 