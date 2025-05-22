namespace MapHive.Models.Data;

/// <summary>
/// Wrapper for an "update-or-skip" field. Use Set(val) to update (val may be null),
/// or Skip() to leave the column untouched.
/// </summary>
public readonly struct DynamicValue<T>
{
    public bool IsAssigned { get; }
    public T? Value { get; }
    private DynamicValue(bool isAssigned, T? value)
    {
        (IsAssigned, Value) = (isAssigned, value);
    }

    public static DynamicValue<T> Set(T v)
    {
        return new(true, v);
    }

    public static DynamicValue<T> Unassigned()
    {
        return new(false, default);
    }

    public DynamicValue<object?> AsGeneric()
    {
        return new(IsAssigned, Value);
    }
}
