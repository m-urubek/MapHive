namespace MapHive.Models.PageModels;

using System.ComponentModel.DataAnnotations;
using MapHive.Attributes;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

public class ThreadMessagePageModel
{
    [Required]
    [StringLength(4000)]
    public required string? MessageText { get; set; }

    [Repopulated("Partials/_ReplyFormPartial")]
    [ValidateNever]
    // For display purposes
    public required string ThreadName { get; set; }
}
