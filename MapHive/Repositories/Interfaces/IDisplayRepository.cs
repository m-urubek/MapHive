namespace MapHive.Repositories.Interfaces
{
    public interface IDisplayRepository
    {
        /// <summary>
        /// Gets all data for a specific item from the specified table
        /// </summary>
        /// <param name="tableName">Name of the table</param>
        /// <param name="id">ID of the item</param>
        /// <returns>Dictionary with column names as keys and values as string values</returns>
        Task<Dictionary<string, string>> GetItemDataAsync(string tableName, int id);

        /// <summary>
        /// Validates if a table exists in the database
        /// </summary>
        /// <param name="tableName">Name of the table to validate</param>
        /// <returns>True if the table exists, false otherwise</returns>
        Task<bool> TableExistsAsync(string tableName);
    }
}