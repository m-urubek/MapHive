namespace MapHive.Controllers;

using MapHive.Models.Exceptions;
using MapHive.Models.PageModels;
using MapHive.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
public class DisplayController(IDisplayPageRepository _displayPageRepository) : Controller
{

    [HttpGet("Display/{tableName:required}/{id:int:required}")]
    [Authorize(Roles = "Admin,2")]
    public async Task<IActionResult> Index(string tableName, int id)
    {
        return string.IsNullOrWhiteSpace(tableName)
            ? throw new PublicWarningException("Table name is required")
            : id < 1
            ? throw new PublicWarningException("Invalid ID")
            : View("DisplayPage", model: new DisplayItemPageModel()
            {
                Data = await _displayPageRepository.GetItemDataOrThrowAsync(tableName: tableName, id: id),
            });
    }
}
