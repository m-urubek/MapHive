using MapHive.Models.ViewModels;

namespace MapHive.Services
{
    public interface IDisplayService
    {
        Task<bool> TableExistsAsync(string tableName);
        Task<DisplayItemViewModel> GetItemAsync(string tableName, int id);
    }
}