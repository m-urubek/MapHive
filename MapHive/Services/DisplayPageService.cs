namespace MapHive.Services;

using MapHive.Models.Exceptions;
using MapHive.Models.PageModels;
using MapHive.Repositories;

public class DisplayPageService(IDisplayPageRepository displayPageRepository) : IDisplayPageService
{
    private readonly IDisplayPageRepository _displayPageRepository = displayPageRepository;

    /// <summary>
    /// Retrieves detailed item data with metadata for display in views.
    /// </summary>
    public async Task<DisplayItemPageModel> GetItemAsync(string tableName, int id)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(value: tableName))
        {
            throw new RedUserException("Table name is required");
        }

        // Check if table exists
        bool exists = await _displayPageRepository.TableExistsAsync(tableName: tableName);
        if (!exists)
        {
            throw new RedUserException($"Table '{tableName}' does not exist");
        }

        // Retrieve the data
        Dictionary<string, string> data = await _displayPageRepository.GetItemDataOrThrowAsync(tableName: tableName, id: id);

        // Build view model
        return new()
        {
            Data = data,
        };
    }
}
