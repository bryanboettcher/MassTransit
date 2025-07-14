namespace MassTransit.Persistence.SqlServer.Extensions
{
    using System.Data;
    using Microsoft.Data.SqlClient;


    public static class SqlDataReaderExtensions
    {
        public static Guid? GetGuidOrNull(this SqlDataReader reader, string columnName)
            => reader.IsDBNull(columnName) ? null : reader.GetGuid(columnName);

        public static string? GetStringOrNull(this SqlDataReader reader, string columnName)
            => reader.IsDBNull(columnName) ? null : reader.GetString(columnName);

        public static byte? GetByteOrNull(this SqlDataReader reader, string columnName)
            => reader.IsDBNull(columnName) ? null : reader.GetByte(columnName);

        public static int? GetInt32OrNull(this SqlDataReader reader, string columnName)
            => reader.IsDBNull(columnName) ? null : reader.GetInt32(columnName);

        public static long? GetInt64OrNull(this SqlDataReader reader, string columnName)
            => reader.IsDBNull(columnName) ? null : reader.GetInt64(columnName);

        public static short? GetInt16OrNull(this SqlDataReader reader, string columnName)
            => reader.IsDBNull(columnName) ? null : reader.GetInt16(columnName);

        public static decimal? GetDecimalOrNull(this SqlDataReader reader, string columnName)
            => reader.IsDBNull(columnName) ? null : reader.GetDecimal(columnName);

        public static float? GetFloatOrNull(this SqlDataReader reader, string columnName)
            => reader.IsDBNull(columnName) ? null : reader.GetFloat(columnName);

        public static double? GetDoubleOrNull(this SqlDataReader reader, string columnName)
            => reader.IsDBNull(columnName) ? null : reader.GetDouble(columnName);

        public static DateTime? GetDateTimeOrNull(this SqlDataReader reader, string columnName)
            => reader.IsDBNull(columnName) ? null : reader.GetDateTime(columnName);

        public static DateTimeOffset? GetDateTimeOffsetOrNull(this SqlDataReader reader, string columnName)
            => reader.IsDBNull(columnName) ? null : reader.GetDateTimeOffset(reader.GetOrdinal(columnName));

        public static TimeSpan? GetTimeSpanOrNull(this SqlDataReader reader, string columnName)
            => reader.IsDBNull(columnName) ? null : reader.GetTimeSpan(reader.GetOrdinal(columnName));

        public static Uri? GetUri(this SqlDataReader reader, string columnName)
            => reader.IsDBNull(columnName) ? null : new Uri(reader.GetString(columnName));
    }
}
