namespace MapHive.ModelBinding;

using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

public class DefaultRequiredErrorMessageProvider : IValidationMetadataProvider
{
    public void CreateValidationMetadata(ValidationMetadataProviderContext context)
    {
        // Iterate through all validator metadata and replace default RequiredAttribute message
        foreach (RequiredAttribute metadata in context.ValidationMetadata.ValidatorMetadata.OfType<RequiredAttribute>())
        {
            // Only override when no custom message or resource is specified
            if (string.IsNullOrEmpty(metadata.ErrorMessage)
                && metadata.ErrorMessageResourceName == null
                && metadata.ErrorMessageResourceType == null)
            {
                metadata.ErrorMessage = "Required";
            }
        }

        // Override default StringLengthAttribute messages
        foreach (StringLengthAttribute metadata in context.ValidationMetadata.ValidatorMetadata.OfType<StringLengthAttribute>())
        {
            // Only override when no custom message or resource is specified
            if (string.IsNullOrEmpty(metadata.ErrorMessage)
                && metadata.ErrorMessageResourceName == null
                && metadata.ErrorMessageResourceType == null)
            {
                // FormatErrorMessage will replace {0} with the field name and {1} with the maximum length
                metadata.ErrorMessage = "Maximum length is {1}!";
            }
        }
    }
}