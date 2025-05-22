namespace MapHive;

using System.Text;

public static class StringBuilderExtensions
{
    public static void AppendLineIfNotNullOrEmpty(this StringBuilder sb, string? value)
    {
        if (!string.IsNullOrEmpty(value: value))
        {
            _ = sb.AppendLine(value: value);
        }
    }
    public static void AppendLineIfNotNullOrWhitespace(this StringBuilder sb, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value: value))
        {
            _ = sb.AppendLine(value: value);
        }
    }
}
