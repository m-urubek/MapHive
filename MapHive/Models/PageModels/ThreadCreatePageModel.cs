namespace MapHive.Models.PageModels;

using System.ComponentModel.DataAnnotations;
using MapHive.Attributes;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

public class ThreadCreatePageModel
{
    [Required]
    [StringLength(100)]
    public required string? ThreadName { get; set; }

    [Required]
    [StringLength(1000)]
    public required string? InitialMessage { get; set; }

    [Repopulated("Discussion/Create")]
    [ValidateNever]
    public required string LocationName { get; set; }
}
