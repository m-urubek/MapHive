namespace MapHive.Services;

using MapHive.Models.PageModels;

public interface IDisplayPageService
{
    public Task<DisplayItemPageModel> GetItemAsync(string tableName, int id);
}
