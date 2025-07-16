namespace MassTransit.Persistence.MySql.Extensions
{
    using System.Data;
    using MySqlConnector;


    public static class MySqlDataReaderExtensions
    {
        public static Guid? GetGuidOrNull(this MySqlDataReader reader, string columnName)
        {
            return reader.IsDBNull(columnName) ? null : reader.GetGuid(columnName);
        }

        public static string? GetStringOrNull(this MySqlDataReader reader, string columnName)
        {
            return reader.IsDBNull(columnName) ? null : reader.GetString(columnName);
        }

        public static byte? GetByteOrNull(this MySqlDataReader reader, string columnName)
        {
            return reader.IsDBNull(columnName) ? null : reader.GetByte(columnName);
        }

        public static int? GetInt32OrNull(this MySqlDataReader reader, string columnName)
        {
            return reader.IsDBNull(columnName) ? null : reader.GetInt32(columnName);
        }

        public static long? GetInt64OrNull(this MySqlDataReader reader, string columnName)
        {
            return reader.IsDBNull(columnName) ? null : reader.GetInt64(columnName);
        }

        public static short? GetInt16OrNull(this MySqlDataReader reader, string columnName)
        {
            return reader.IsDBNull(columnName) ? null : reader.GetInt16(columnName);
        }

        public static decimal? GetDecimalOrNull(this MySqlDataReader reader, string columnName)
        {
            return reader.IsDBNull(columnName) ? null : reader.GetDecimal(columnName);
        }

        public static float? GetFloatOrNull(this MySqlDataReader reader, string columnName)
        {
            return reader.IsDBNull(columnName) ? null : reader.GetFloat(columnName);
        }

        public static double? GetDoubleOrNull(this MySqlDataReader reader, string columnName)
        {
            return reader.IsDBNull(columnName) ? null : reader.GetDouble(columnName);
        }

        public static DateTime? GetDateTimeOrNull(this MySqlDataReader reader, string columnName)
        {
            return reader.IsDBNull(columnName) ? null : reader.GetDateTime(columnName);
        }

        public static DateTime? GetDateTimeOffsetOrNull(this MySqlDataReader reader, string columnName)
        {
            return reader.IsDBNull(columnName) ? null : reader.GetDateTime(columnName);
        }

        public static TimeSpan? GetTimeSpanOrNull(this MySqlDataReader reader, string columnName)
        {
            return reader.IsDBNull(columnName) ? null : reader.GetTimeSpan(reader.GetOrdinal(columnName));
        }

        public static Uri? GetUri(this MySqlDataReader reader, string columnName)
        {
            return reader.IsDBNull(columnName) ? null : new Uri(reader.GetString(columnName));
        }
    }
}
