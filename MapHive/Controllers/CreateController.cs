namespace MapHive.Controllers;

using MapHive.Models.Exceptions;
using MapHive.Models.PageModels;
using MapHive.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
public class CreateController(ICreatePageRepository _createPageRepository) : Controller
{
    [HttpGet("Create/{tableName:required}")]
    [Authorize(Roles = "Admin,2")]
    public async Task<IActionResult> Index(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new PublicWarningException("Table name is required");

        CreateItemPageModel model = await _createPageRepository.GetTableSchemaForCreateAsync(
            tableName: tableName);

        return View("CreatePage", model: model);
    }

    [HttpPost("Create/{tableName:required}")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,2")]
    public async Task<IActionResult> Save(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new PublicWarningException("Table name is required");

        // Get the schema to know which fields exist
        CreateItemPageModel schemaModel = await _createPageRepository.GetTableSchemaForCreateAsync(
            tableName: tableName);

        // Collect values from form
        Dictionary<string, string?> values = new();

        foreach (KeyValuePair<string, CreateFieldInfo> field in schemaModel.Fields)
        {
            // Skip primary key fields - they are auto-generated
            if (field.Value.IsPrimaryKey)
                continue;

            // Get the value from the form using the column name
            string formKey = $"field_{field.Key}";
            if (Request.Form.TryGetValue(formKey, out Microsoft.Extensions.Primitives.StringValues formValue))
            {
                string? newValue = formValue.ToString();
                values[field.Key] = string.IsNullOrEmpty(newValue) ? null : newValue;
            }
        }

        // Perform the insert
        int newId = await _createPageRepository.CreateItemAsync(
            tableName: tableName,
            values: values);

        // Redirect to display page after successful creation
        return RedirectToAction(
            actionName: "Index",
            controllerName: "Display",
            routeValues: new { tableName, id = newId });
    }
}

