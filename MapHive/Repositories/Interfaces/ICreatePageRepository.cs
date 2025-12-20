namespace MapHive.Repositories;

using MapHive.Models.PageModels;

public interface ICreatePageRepository
{
    /// <summary>
    /// Gets the schema information for creating a new item in the specified table
    /// </summary>
    /// <param name="tableName">Name of the table</param>
    /// <returns>CreateItemPageModel with field information</returns>
    Task<CreateItemPageModel> GetTableSchemaForCreateAsync(string tableName);

    /// <summary>
    /// Creates a new item in the specified table
    /// </summary>
    /// <param name="tableName">Name of the table</param>
    /// <param name="values">Dictionary of column names to values</param>
    /// <returns>The ID of the newly created item</returns>
    Task<int> CreateItemAsync(string tableName, Dictionary<string, string?> values);

    /// <summary>
    /// Gets the primary key column name for a table
    /// </summary>
    /// <param name="tableName">Name of the table</param>
    /// <returns>Primary key column name</returns>
    Task<string> GetPrimaryKeyColumnAsync(string tableName);
}

