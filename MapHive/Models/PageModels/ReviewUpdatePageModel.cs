namespace MapHive.Models.PageModels;

using System.ComponentModel.DataAnnotations;
using MapHive.Attributes;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

/// <summary>used for create and update, view needs own</summary>
public class ReviewUpdatePageModel
{
    [Required]
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
    public required int? Rating { get; set; }

    [Required]
    [StringLength(500)]
    public required string? ReviewText { get; set; }

    [BindRequired]
    public required bool IsAnonymous { get; set; }

    // For display purposes
    [Repopulated("Review/Create")]
    [Repopulated("Review/Edit")]
    [ValidateNever]
    public required string LocationName { get; set; }
}
