namespace MapHive.Models.PageModels;

using System.ComponentModel.DataAnnotations;
using MapHive.Attributes;
using MapHive.Models.Data.DbTableModels;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

/// <summary>
/// PageModel for the Map/Add page, containing the create DTO and list of categories.
/// </summary>
public class LocationUpdatePageModel
{
    [Required]
    [StringLength(100)]
    [NoHtml]
    public required string? Name { get; set; }

    [StringLength(500)]
    [NoHtml]
    public required string? Description { get; set; }

    [Required]
    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
    public required double? Latitude { get; set; }

    [Required]
    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
    public required double? Longitude { get; set; }

    [StringLength(200)]
    [NoHtml]
    public required string? Address { get; set; }

    [DataType(DataType.Url)]
    [StringLength(255)]
    [NoHtml]
    public required string? Website { get; set; }

    [StringLength(50)]
    [NoHtml]
    public required string? PhoneNumber { get; set; }

    [BindRequired]
    [Required]
    public required bool IsAnonymous { get; set; }

    [Required]
    public required int? CategoryId { get; set; }

    [Repopulated("Map/Add")]
    [ValidateNever]
    public required IEnumerable<CategoryAtomic> Categories { get; set; }
}
