using System;

namespace SharpOrm.Builder.DataTranslation
{
    [Obsolete("It will be removed in version 2.x.x.")]
    internal static class TimeProcessor
    {
        public static DateTime ToDatabase(this DateTime date, IQueryConfig config)
        {
            return DateTranslation.ConvertDate(config.LocalTimeZone, GetTimeZone(config.DateKind), date);
        }

        public static DateTime FromDatabase(this DateTime date, IQueryConfig config)
        {
            return DateTranslation.ConvertDate(GetTimeZone(config.DateKind), config.LocalTimeZone, date);
        }

        private static TimeZoneInfo GetTimeZone(DateTimeKind kind)
        {
            switch (kind)
            {
                case DateTimeKind.Utc: return TimeZoneInfo.Utc;
                default: return TimeZoneInfo.Local;
            }
        }
    }
}