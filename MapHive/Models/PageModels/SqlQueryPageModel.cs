namespace MapHive.Models.PageModels;

using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

public class SqlQueryPageModel
{
    [Required]
    public required string? Query { get; set; }

    [ValidateNever]
    public required int? RowsAffected { get; set; }
    [ValidateNever]
    public required DataTable? DataTable { get; set; }
    [ValidateNever]
    public required string? Message { get; set; }
}
