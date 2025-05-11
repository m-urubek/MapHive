namespace MapHive.Utilities
{
    using System;
    using System.Data;
    using MapHive.Models.Enums;
    using MapHive.Services;

    public static class DataRowMappingExtensions
    {
        public static T GetValueOrDefault<T>(this DataRow row, ILogManagerService logManager, string tableName, string columnName, bool isRequired, Func<object, T> converter, T defaultValue = default!)
        {
            object value = row[columnName];
            if (value == DBNull.Value)
            {
                if (isRequired)
                    logManager.Log(LogSeverity.Warning, $"Value for column \"{columnName}\" in table \"{tableName}\" cannot be null");
                return defaultValue;
            }
            return converter(value);
        }
    }
}
