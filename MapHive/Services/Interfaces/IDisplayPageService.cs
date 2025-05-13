namespace MapHive.Services
{
    using MapHive.Models.ViewModels;

    public interface IDisplayPageService
    {
        Task<bool> TableExistsAsync(string tableName);
        Task<DisplayItemViewModel> GetItemAsync(string tableName, int id);
    }
}