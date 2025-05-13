namespace MapHive.Repositories
{
    using MapHive.Models.RepositoryModels;

    public interface ILogRepository
    {
        /// <summary>
        /// Gets the total count of logs matching the search term
        /// </summary>
        /// <param name="searchTerm">Optional search term to filter logs</param>
        /// <returns>Total count of logs</returns>
        Task<int> GetTotalLogsCountAsync(
            string searchTerm = "");

        Task<int> CreateLogRowAsync(LogCreate logCreate);
    }
}
