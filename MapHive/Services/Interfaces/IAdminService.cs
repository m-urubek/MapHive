namespace MapHive.Services;

using MapHive.Models.PageModels;

public interface IAdminService
{
    // SQL execution
    Task<SqlQueryPageModel> ExecuteSqlQueryAsync(string query);
}
