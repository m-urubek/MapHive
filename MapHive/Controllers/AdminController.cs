namespace MapHive.Controllers;

using MapHive.Models.PageModels;
using MapHive.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
public class AdminController(
    IAdminService _adminService) : Controller
{
    [HttpGet("AdminPanel")]
    [Authorize(Roles = "Admin,2")]
    public IActionResult Index()
    {
        return View();
    }

    #region SQL Execution

    [HttpGet("AdminPanel/SqlQuery")]
    [Authorize(Roles = "Admin,2")]
    public IActionResult SqlQuery()
    {
        return View(new SqlQueryPageModel()
        {
            Query = "",
            RowsAffected = null,
            DataTable = null,
            Message = null
        });
    }

    [HttpPost("AdminPanel/SqlQuery")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,2")]
    public async Task<IActionResult> SqlQuery(SqlQueryPageModel sqlQueryPageModel)
    {
        if (!ModelState.IsValid)
        {
            return View(model: sqlQueryPageModel);
        }

        SqlQueryPageModel resultModel = await _adminService.ExecuteSqlQueryAsync(query: sqlQueryPageModel.Query!);
        return View(model: resultModel);
    }

    #endregion
}
