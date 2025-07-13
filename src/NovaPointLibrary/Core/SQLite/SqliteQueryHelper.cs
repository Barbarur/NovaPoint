using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Core.SQLite
{
    internal class SqliteQueryHelper
    {
        internal static string GetCreateTableQuery(Type type)
        {
            StringBuilder sbQuery = new StringBuilder();
            sbQuery.Append($"CREATE TABLE IF NOT EXISTS {type.Name} (");

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                string columnName = property.Name;
                string columnType = GetSqlType(property.PropertyType);
                sbQuery.Append($" {columnName} {columnType},");
            }

            sbQuery.Length--;
            sbQuery.Append(");");

            return sbQuery.ToString();
        }

        internal static string GetInsertQuery<T>(T obj)
        {
            Type type = obj.GetType();

            return GetInsertQuery(obj, type.Name);
        }

        internal static string GetInsertQuery<T>(T obj, string tableName)
        {
            Type type = obj.GetType();

            StringBuilder sbColumns = new StringBuilder();
            sbColumns.Append("(");
            StringBuilder sbValues = new StringBuilder();
            sbValues.Append("(");

            foreach (var propertyInfo in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                sbColumns.Append($"{propertyInfo.Name},");

                object? propertyValue = propertyInfo.GetValue(obj);
                string stringValue = propertyValue?.ToString() ?? string.Empty;
                string sanitizedValue = stringValue.Replace("'", "''");
                sbValues.Append($"'{sanitizedValue}',");
            }
            sbColumns.Length--;
            sbColumns.Append(")");
            sbValues.Length--;
            sbValues.Append(")");

            string insertQuery = $"INSERT INTO {tableName} {sbColumns} VALUES {sbValues}";

            return insertQuery;
        }

        static string GetSqlType(Type type)
        {
            if (type == typeof(int)) return "INTEGER";
            if (type == typeof(string)) return "TEXT";
            if (type == typeof(bool)) return "BOOLEAN";
            if (type == typeof(DateTime)) return "TEXT"; // SQLite uses TEXT for datetime values
            // Add more type mappings as needed
            return "TEXT";  // Default to TEXT for unsupported types
        }
    }
}
