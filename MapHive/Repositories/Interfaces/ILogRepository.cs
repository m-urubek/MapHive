using MapHive.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MapHive.Repositories.Interfaces
{
    public interface ILogRepository
    {
        /// <summary>
        /// Gets all logs with pagination and sorting
        /// </summary>
        /// <param name="page">The page number (1-based)</param>
        /// <param name="pageSize">The number of items per page</param>
        /// <param name="searchTerm">Optional search term to filter logs</param>
        /// <param name="sortField">Optional field to sort by</param>
        /// <param name="sortDirection">Sort direction ('asc' or 'desc')</param>
        /// <returns>List of logs</returns>
        Task<IEnumerable<Log>> GetLogsAsync(
            int page = 1, 
            int pageSize = 20, 
            string searchTerm = "",
            string sortField = "Timestamp", 
            string sortDirection = "desc");

        /// <summary>
        /// Gets the total count of logs matching the search term
        /// </summary>
        /// <param name="searchTerm">Optional search term to filter logs</param>
        /// <returns>Total count of logs</returns>
        Task<int> GetTotalLogsCountAsync(
            string searchTerm = "");

        /// <summary>
        /// Gets a log by its ID
        /// </summary>
        /// <param name="id">The log ID</param>
        /// <returns>The log or null if not found</returns>
        Task<Log?> GetLogByIdAsync(int id);

        /// <summary>
        /// Gets all log severity levels
        /// </summary>
        /// <returns>List of log severity levels</returns>
        Task<IEnumerable<LogSeverity>> GetLogSeveritiesAsync();
    }
} 