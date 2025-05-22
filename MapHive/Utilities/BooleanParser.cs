namespace MapHive.Utilities;

public static class BooleanParser
{
    public static bool Parse(string value)
    {
        return bool.TryParse(value, out bool result)
            ? result
            : value == "1" || (value == "0"
                ? false
                : throw new Exception($"(value: '{value}') is not a valid boolean representation (true, false, 1, or 0).")
            );
    }
}