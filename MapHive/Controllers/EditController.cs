namespace MapHive.Controllers;

using MapHive.Models.Exceptions;
using MapHive.Models.PageModels;
using MapHive.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
public class EditController(IEditPageRepository _editPageRepository) : Controller
{
    [HttpGet("Edit/{tableName:required}/{id:int:required}")]
    [Authorize(Roles = "Admin,2")]
    public async Task<IActionResult> Index(string tableName, int id)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new PublicWarningException("Table name is required");

        if (id < 1)
            throw new PublicWarningException("Invalid ID");

        EditItemPageModel model = await _editPageRepository.GetItemForEditOrThrowAsync(
            tableName: tableName,
            id: id);

        return View("EditPage", model: model);
    }

    [HttpPost("Edit/{tableName:required}/{id:int:required}")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,2")]
    public async Task<IActionResult> Save(string tableName, int id)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new PublicWarningException("Table name is required");

        if (id < 1)
            throw new PublicWarningException("Invalid ID");

        // Get the original model to know which fields exist
        EditItemPageModel originalModel = await _editPageRepository.GetItemForEditOrThrowAsync(
            tableName: tableName,
            id: id);

        // Collect updated values from form
        Dictionary<string, string?> updatedValues = new();

        foreach (KeyValuePair<string, EditFieldInfo> field in originalModel.Fields)
        {
            // Skip primary key fields - they shouldn't be editable
            if (field.Value.IsPrimaryKey)
                continue;

            // Get the value from the form using the column name
            string formKey = $"field_{field.Key}";
            if (Request.Form.TryGetValue(formKey, out Microsoft.Extensions.Primitives.StringValues formValue))
            {
                string? newValue = formValue.ToString();
                updatedValues[field.Key] = string.IsNullOrEmpty(newValue) ? null : newValue;
            }
        }

        // Perform the update
        await _editPageRepository.UpdateItemAsync(
            tableName: tableName,
            id: id,
            updatedValues: updatedValues);

        // Redirect back to display page after successful update
        return RedirectToAction(
            actionName: "Index",
            controllerName: "Display",
            routeValues: new { tableName, id });
    }
}

