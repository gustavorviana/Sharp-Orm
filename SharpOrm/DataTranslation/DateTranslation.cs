﻿using System;
using System.Globalization;

namespace SharpOrm.DataTranslation
{
    public class DateTranslation : ISqlTranslation
    {
        #region Fields\Properties
        private TimeZoneInfo _dbTimeZone = TimeZoneInfo.Local;
        private TimeZoneInfo _codeTimeZone = TimeZoneInfo.Local;
        internal const string Format = "yyyy-MM-dd HH:mm:ss";

        /// <summary>
        /// Timezone in which dates should be stored in the database.
        /// </summary>
        /// <value><see cref="TimeZoneInfo.Local"/></value>
        public TimeZoneInfo DbTimeZone
        {
            get => _dbTimeZone;
            set => _dbTimeZone = value ?? TimeZoneInfo.Local;
        }

        /// <summary>
        /// Timezone in which dates should be converted to work within the code.
        /// </summary>
        /// <value><see cref="TimeZoneInfo.Local"/></value>
        public TimeZoneInfo CodeTimeZone
        {
            get => _codeTimeZone;
            set => _codeTimeZone = value ?? TimeZoneInfo.Local;
        }
        #endregion

        #region ISqlTranslation
        public bool CanWork(Type type) => type == typeof(DateTimeOffset) || type == typeof(DateTime) || type == typeof(TimeSpan) || type == typeof(string) || type == typeof(FreezedDate);

        public object FromSqlValue(object value, Type expectedType)
        {
            if (expectedType == typeof(FreezedDate))
                throw new NotSupportedException();

            if (expectedType == typeof(DateTimeOffset))
                return this.LoadDateTimeOffset(value);

            if (expectedType == typeof(DateTime))
                return ParseDateTimeFromDb(value);

            if (expectedType == typeof(TimeSpan))
                return ParseTimespanFromDb(value);

            return value;
        }

        public object ToSqlValue(object value, Type type)
        {
            if (value is FreezedDate fDate)
                return fDate.Value;

            if (value is DateTime date)
                return DateToDb(date, type);

            if (value is DateTimeOffset tOffset)
                return DateOffsetToDb(tOffset, type);

            if (value is TimeSpan time && type == typeof(string))
                return time.ToString();

            return value;
        }

        private DateTimeOffset? LoadDateTimeOffset(object value)
        {
            if (value is DateTimeOffset offset)
                return offset;

            if (value is DateTime date)
                return new DateTimeOffset(date, DbTimeZone.BaseUtcOffset);

            if (value is string str && DateTimeOffset.TryParse(str, out var strOffset))
                return strOffset;

            return null;
        }

        #endregion

        #region FromDb
        private object ToExpectedType(DateTime date, Type type)
        {
            if (type == typeof(DateTime))
                return date;

            if (type == typeof(TimeSpan))
                return date.TimeOfDay;

            if (type == typeof(string))
                return date.ToString(Format);

            return date;
        }

        private object ParseTimespanFromDb(object obj)
        {
            if (obj is string strTime && TimeSpan.TryParse(strTime, out var time))
                return time;

            obj = ParseDateTimeFromDb(obj);

            if (obj is DateTime date)
                return date.TimeOfDay;

            return obj;
        }

        private object ParseDateTimeFromDb(object obj)
        {
            if (obj is DateTimeOffset offset)
            {
                if (DbTimeZone.Equals(CodeTimeZone)) return offset.DateTime;

                return TimeZoneInfo.ConvertTime(offset.UtcDateTime, CodeTimeZone);
            }

            if (obj is DateTime date)
                return ConvertDate(DbTimeZone, CodeTimeZone, date);

            if (obj is string strDate)
                return ParseDateStringFromDb(strDate);

            return obj;
        }

        private DateTime? ParseDateStringFromDb(string dateStr)
        {
            if (DateTime.TryParseExact(dateStr, Format, null, DateTimeStyles.None, out DateTime date))
                return ConvertDate(DbTimeZone, CodeTimeZone, date);

            return null;
        }
        #endregion

        #region ToDb
        private object DateToDb(DateTime date, Type type)
        {
            if (type == typeof(DateTimeOffset))
                return new DateTimeOffset(date, CodeTimeZone.BaseUtcOffset);

            return ToExpectedType(ConvertDate(CodeTimeZone, DbTimeZone, date), type);
        }

        private object DateOffsetToDb(DateTimeOffset dtOffset, Type type)
        {
            if (type == typeof(DateTimeOffset))
                return dtOffset;

            return ToExpectedType(TimeZoneInfo.ConvertTimeFromUtc(dtOffset.UtcDateTime, DbTimeZone), type);
        }
        #endregion

        internal static DateTime ConvertDate(TimeZoneInfo sourceZone, TimeZoneInfo targetZone, DateTime date)
        {
            if (sourceZone.Equals(targetZone) || date.Kind == DateTimeKind.Utc && targetZone.Equals(TimeZoneInfo.Utc))
                return date;

            if (date.Kind == DateTimeKind.Utc)
                return TimeZoneInfo.ConvertTimeFromUtc(date, targetZone);

            date = DateTime.SpecifyKind(date, GetZoneKind(sourceZone));
            return TimeZoneInfo.ConvertTime(date, sourceZone, targetZone);
        }

        private static DateTimeKind GetZoneKind(TimeZoneInfo zone)
        {
            if (zone == TimeZoneInfo.Utc)
                return DateTimeKind.Utc;

            if (zone == TimeZoneInfo.Local)
                return DateTimeKind.Local;

            return DateTimeKind.Unspecified;
        }
    }
}
