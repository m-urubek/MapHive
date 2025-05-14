namespace MapHive.Utilities.Extensions
{
    using System;
    using System.Data;
    using MapHive.Models.Enums;
    using MapHive.Services;

    public static class DataRowMappingExtensions
    {
        public static T? GetValueNullable<T>(this DataRow row, string columnName) where T : struct
        {
            return GetValueNullableInternal<T>(row: row, columnName: columnName);
        }

        public static string? GetAsNullableString(this DataRow row, string columnName)
        {
            return GetValueNullableInternal<string?>(row: row, columnName: columnName);
        }

        public static string? ToNullableString(this DataRow row, string columnName)
        {
            return !row.Table.Columns.Contains(columnName)
                ? throw new Exception($"Column \"{columnName}\" not found in table \"{row.Table.TableName}\"")
                : row[columnName].ToString();
        }

        private static T? GetValueNullableInternal<T>(this DataRow row, string columnName)
        {
            if (!row.Table.Columns.Contains(columnName))
                throw new Exception($"Column \"{columnName}\" not found in table \"{row.Table.TableName}\"");

            object value = row[columnName];

            if (value == null || value == DBNull.Value)
            {
                return default;
            }
            else
            {
                value = ConvertSpecific(value);
                return value is T typedValue
                ? (T?)typedValue
                : throw new Exception($"{nameof(GetValueNullable)}: Value of type {value.GetType().Name} for column \"{columnName}\" is not a {typeof(T).Name}");
            }
        }

        public static T GetValueOrThrow<T>(this DataRow row, string columnName)
        {
            if (!row.Table.Columns.Contains(columnName))
                throw new Exception($"Column \"{columnName}\" not found in table \"{row.Table.TableName}\"");

            object value = row[columnName];

            if (value == null || value == DBNull.Value)
            {
                throw new Exception($"Value for column \"{columnName}\" in table \"{row.Table.TableName}\" cannot be null");
            }
            else
            {
                value = ConvertSpecific(value);
                return value is not T ? throw new Exception($"{nameof(DataRowMappingExtensions)}.{nameof(GetValueOrThrow)}: Value of type {typeof(T).Name} for column \"{columnName}\" in table \"{row.Table.TableName}\" is not a {typeof(T).Name}")
                : (T)value;
            }
        }

        public static object ConvertSpecific(object value) {
            return value switch
            {
                long => Convert.ToInt32(value),
                string valueAsString => DateTime.TryParse(valueAsString, out DateTime valueAsDateTime) ? valueAsDateTime : value,
                _ => value,
            };
        }
    }
}
