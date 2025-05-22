namespace MapHive;

using System;
using System.Data;
using MapHive.Utilities;

public static class DataRowMappingExtensions
{
    public static T GetValueThrowNotPresentOrNull<T>(this DataRow row, string columnName)
    {
        row.EnsureExists(columnName);
        object? value = row[columnName];
        return value == null || value == DBNull.Value
            ? throw new NoNullAllowedException($"Value for column \"{columnName}\" in table \"{row.Table.TableName}\" cannot be null")
            : row.GetValueThrowNotPresent<T>(columnName) ?? throw new Exception($"{nameof(DataRowMappingExtensions)}.{nameof(GetValueThrowNotPresentOrNull)}: Value of type {typeof(T).Name} for column \"{columnName}\" in table \"{row.Table.TableName}\" is not a {typeof(T).Name}");
    }

    public static T? GetValueThrowNotPresent<T>(this DataRow row, string columnName)
    {
        row.EnsureExists(columnName);
        object? value = row[columnName];
        if (value == null || value == DBNull.Value)
        {
            return default!;
        }
        else
        {
            value = ConvertSpecific<T>(value);
            return value is not T ? throw new Exception($"{nameof(DataRowMappingExtensions)}.{nameof(GetValueThrowNotPresent)}: Value of type {typeof(T).Name} for column \"{columnName}\" in table \"{row.Table.TableName}\" is not a {typeof(T).Name}")
            : (T)value;
        }
    }

    public static T ConvertSpecific<T>(object value)
    {
        switch (value)
        {
            case long valueAsLong:
                int valueAsInt = Convert.ToInt32(valueAsLong);
                return typeof(T) == typeof(bool)
                    ? (T)(object)BooleanParser.Parse(valueAsInt.ToString())
                    : (T)(object)valueAsInt;
            case string valueAsString:
                return (T)(DateTime.TryParse(valueAsString, out DateTime valueAsDateTime) ? valueAsDateTime : value);
            default:
                return (T)value;
        }
        ;
    }

    public static void EnsureExists(this DataRow row, string columnName)
    {
        if (!row.Table.Columns.Contains(columnName))
            throw new Exception($"Column \"{columnName}\" not found in table \"{row.Table.TableName}\"");
    }
}
