using System.Text;

namespace MapHive.Utilities.Extensions
{
    public static class StringBuilderExtensions
    {
        public static void AppendLineIfNotNullOrEmpty(this StringBuilder sb, string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _ = sb.AppendLine(value);
            }
        }
        public static void AppendLineIfNotNullOrWhitespace(this StringBuilder sb, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                _ = sb.AppendLine(value);
            }
        }
    }
}
