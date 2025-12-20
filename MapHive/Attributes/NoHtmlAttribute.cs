namespace MapHive.Attributes;

using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;

/// <summary>
/// Validation attribute that prevents HTML/script injection in user input.
/// Uses the built-in .NET HtmlEncoder to detect content that would be encoded differently,
/// indicating the presence of potentially dangerous characters.
/// This provides defense-in-depth against XSS attacks at the input level.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public class NoHtmlAttribute : ValidationAttribute
{
    public NoHtmlAttribute() : base("HTML tags and scripts are not allowed in this field.")
    {
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return ValidationResult.Success;
        }

        if (value is not string str)
        {
            return ValidationResult.Success;
        }

        if (string.IsNullOrWhiteSpace(str))
        {
            return ValidationResult.Success;
        }

        // Use the built-in .NET HtmlEncoder to check if the string contains
        // characters that would be encoded (meaning they contain HTML/script content).
        // This is more reliable than custom regex patterns.
        string encoded = HtmlEncoder.Default.Encode(str);

        // If the encoded version differs from the original, it contains HTML-sensitive characters
        if (!string.Equals(a: str, b: encoded, comparisonType: StringComparison.Ordinal))
        {
            return new ValidationResult(
                ErrorMessage ?? "HTML tags and scripts are not allowed in this field.",
                validationContext.MemberName != null ? [validationContext.MemberName] : null);
        }

        return ValidationResult.Success;
    }
}

