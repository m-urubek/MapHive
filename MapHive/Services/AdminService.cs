namespace MapHive.Services;

using System.Data;
using MapHive.Models.PageModels;
using MapHive.Singletons;

public class AdminService(
    ISqlClientSingleton _sqlClientSingleton) : IAdminService
{
    public async Task<SqlQueryPageModel> ExecuteSqlQueryAsync(string query)
    {
        int? rowsAffectedCount = null;
        DataTable? dataTable = null;
        string message;
        try
        {
            if (query.StartsWith(value: "SELECT", comparisonType: StringComparison.OrdinalIgnoreCase))
            {
                dataTable = await _sqlClientSingleton.SelectAsync(query: query);
                rowsAffectedCount = dataTable.Rows.Count;
                message = $"Query executed successfully. {rowsAffectedCount} rows returned.";
            }
            else if (query.StartsWith(value: "INSERT", comparisonType: StringComparison.OrdinalIgnoreCase))
            {
                int createdId = await _sqlClientSingleton.InsertAsync(query: query);
                rowsAffectedCount = 1;
                message = $"Insert executed successfully. ID of inserted row: {createdId}";
            }
            else if (query.StartsWith(value: "UPDATE", comparisonType: StringComparison.OrdinalIgnoreCase))
            {
                rowsAffectedCount = await _sqlClientSingleton.UpdateOrThrowAsync(query: query);
                message = $"Update executed successfully. {rowsAffectedCount} rows affected.";
            }
            else if (query.StartsWith(value: "DELETE", comparisonType: StringComparison.OrdinalIgnoreCase))
            {
                rowsAffectedCount = await _sqlClientSingleton.DeleteAsync(query: query);
                message = $"Delete executed successfully. {rowsAffectedCount} rows affected.";
            }
            else
            {
                rowsAffectedCount = await _sqlClientSingleton.AlterAsync(query: query);
                message = "Query executed successfully.";
            }
        }
        catch (Exception ex)
        {
            message = $"Error executing query: {ex.Message}";
        }

        return new SqlQueryPageModel
        {
            Message = message,
            RowsAffected = rowsAffectedCount,
            DataTable = dataTable,
            Query = query
        };
    }
}
