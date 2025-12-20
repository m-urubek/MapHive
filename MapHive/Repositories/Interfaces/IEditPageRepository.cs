namespace MapHive.Repositories;

using MapHive.Models.PageModels;

public interface IEditPageRepository
{
    /// <summary>
    /// Gets all data for a specific item from the specified table for editing
    /// </summary>
    /// <param name="tableName">Name of the table</param>
    /// <param name="id">ID of the item</param>
    /// <returns>EditItemPageModel with field information</returns>
    Task<EditItemPageModel> GetItemForEditOrThrowAsync(string tableName, int id);

    /// <summary>
    /// Updates an item in the specified table
    /// </summary>
    /// <param name="tableName">Name of the table</param>
    /// <param name="id">ID of the item</param>
    /// <param name="updatedValues">Dictionary of column names to new values</param>
    Task UpdateItemAsync(string tableName, int id, Dictionary<string, string?> updatedValues);

    /// <summary>
    /// Gets the primary key column name for a table
    /// </summary>
    /// <param name="tableName">Name of the table</param>
    /// <returns>Primary key column name</returns>
    Task<string> GetPrimaryKeyColumnAsync(string tableName);
}

