namespace MapHive.Models.PageModels;

using System.ComponentModel.DataAnnotations;
using MapHive.Attributes;
using MapHive.Models.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

public class BanUserUpdatePageModel
{
    [Required]
    public required BanType? BanType { get; set; }

    [StringLength(500)]
    public required string? Reason { get; set; }

    [BindRequired]
    [Required]
    public required bool IsPermanent { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Duration must be at least 1 day")]
    public required int? BanDurationDays { get; set; }

    [Repopulated("Account/BanUser")]
    [ValidateNever]
    public required string Username { get; set; }
    [Repopulated("Account/BanUser")]
    [ValidateNever]
    public required AccountTier AccountTier { get; set; }
}
