using SharpOrm.Builder;
using SharpOrm.Msg;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace SharpOrm.DataTranslation
{
    /// <summary>
    /// Class responsible for translating data between the database and code.
    /// </summary>
    public class TranslationRegistry : IEquatable<TranslationRegistry>, ICloneable
    {
        private readonly NativeSqlTranslation _native = new NativeSqlTranslation();
        private static TranslationRegistry _default = new TranslationRegistry();
        private readonly ConcurrentDictionary<Type, TableInfo> _mappedTables = new ConcurrentDictionary<Type, TableInfo>();
        private readonly ConcurrentDictionary<Type, ISqlTranslation> _cachedTranslations = new ConcurrentDictionary<Type, ISqlTranslation>();

        public static TranslationRegistry Default
        {
            get => _default;
            set => _default = value ?? throw new ArgumentNullException(nameof(Default));
        }

        /// <summary>
        /// Indicates whether empty strings should be converted to null values.
        /// </summary>
        public bool EmptyStringToNull
        {
            get => _native.EmptyStringToNull;
            set => _native.EmptyStringToNull = value;
        }

        private ISqlTranslation[] _sqlTranslations = DotnetUtils.EmptyArray<ISqlTranslation>();

        /// <summary>
        /// Custom value translators.
        /// </summary>
        public ISqlTranslation[] Translators
        {
            get => _sqlTranslations;
            set
            {
                _sqlTranslations = value;
                _cachedTranslations.Clear();
            }
        }

        /// <summary>
        /// Format in which the GUID should be read and written in the database.
        /// </summary>
        /// <value>Default value in C#: "D".</value>
        /// <remarks>
        /// <list type="table">
        /// <item>N: 00000000000000000000000000000000</item>
        /// <item>D: 00000000-0000-0000-0000-000000000000</item>
        /// <item>B: {00000000-0000-0000-0000-000000000000}</item>
        /// <item>P: (00000000-0000-0000-0000-000000000000)</item>
        /// <item>X: {0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}}</item>
        /// </list>
        /// </remarks>
        public string GuidFormat
        {
            get => _native.GuidFormat;
            set => _native.GuidFormat = value;
        }

        /// <summary>
        /// Gets or sets the date format used for date translations.
        /// </summary>
        /// <remarks>
        /// The default date format is "yyyy-MM-dd HH:mm:ss".
        /// </remarks>
        public string DateFormat
        {
            get => _native.dateTranslation.Format;
            set => _native.dateTranslation.Format = value;
        }

        /// <summary>
        /// Gets or sets the culture used for numeric translations.
        /// </summary>
        public CultureInfo Culture
        {
            get => _native.numericTranslation.Culture;
            set => _native.numericTranslation.Culture = value;
        }

        /// <summary>
        /// Gets or sets the serialization format for enums.
        /// </summary>
        /// <value>The serialization format for enums.</value>
        public EnumSerialization EnumSerialization
        {
            get => _native.EnumSerialization;
            set => _native.EnumSerialization = value;
        }

        /// <summary>
        /// Timezone in which dates should be stored in the database.
        /// </summary>
        /// <value><see cref="TimeZoneInfo.Local"/></value>
        public TimeZoneInfo DbTimeZone
        {
            get => _native.DbTimeZone;
            set => _native.DbTimeZone = value;
        }

        /// <summary>
        /// Timezone in which dates should be converted to work within the code.
        /// </summary>
        /// <value><see cref="TimeZoneInfo.Local"/></value>
        public TimeZoneInfo TimeZone
        {
            get => _native.TimeZone;
            set => _native.TimeZone = value;
        }

        /// <summary>
        /// Converts a C# value to the database.
        /// </summary>
        /// <param name="value">The value to be converted.</param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException">It is thrown when there is no converter for the requested value.</exception>
        public object ToSql(object value)
        {
            if (value is null || value is DBNull)
                return DBNull.Value;

            return ToSql(value, value.GetType());
        }

        internal object ToSql(object value, Type expectedType)
        {
            if (GetFor(expectedType) is ISqlTranslation conversor)
                return conversor.ToSqlValue(value, expectedType);

            throw new NotSupportedException(string.Format(Messages.TypeNotSupported, expectedType.FullName));
        }


        public bool IsDateOrTime(Type type)
        {
            return _native.dateTranslation.CanWork(type);
        }

        /// <summary>
        /// Converts a value from the database to C#.
        /// </summary>
        /// <param name="value">The value to be converted.</param>
        /// <param name="expectedType">Expected type in C#.</param>
        /// <returns></returns>
        public object FromSql(object value)
        {
            if (value is null || value is DBNull)
                return null;

            Type expectedType = value.GetType();

            if (GetFor(expectedType) is ISqlTranslation conversor)
                return conversor.FromSqlValue(value, expectedType);

            return value;
        }

        /// <summary>
        /// Converts a value from the database to C#.
        /// </summary>
        /// <typeparam name="T">Expected type</typeparam>
        /// <param name="value">The value to be converted.</param>
        /// <returns></returns>
        public T FromSql<T>(object value)
        {
            return (T)FromSql(value, typeof(T));
        }

        /// <summary>
        /// Converts a value from the database to C#.
        /// </summary>
        /// <param name="value">The value to be converted.</param>
        /// <param name="expectedType">Expected type in C#.</param>
        /// <returns></returns>
        public object FromSql(object value, Type expectedType)
        {
            if (value is null || value is DBNull)
                return null;

            expectedType = GetValidTypeFor(expectedType);

            if (GetFor(expectedType) is ISqlTranslation conversor)
                return conversor.FromSqlValue(value, expectedType);

            return value;
        }

        /// <summary>
        /// Retrieves a converter for the specified type.
        /// </summary>
        /// <param name="type">The type to be passed to the converter.</param>
        /// <returns></returns>
        public ISqlTranslation GetFor(Type type)
        {
            type = GetValidTypeFor(type);

            return _cachedTranslations.GetOrAdd(type, pendingTipe =>
            {
                if (Translators?.FirstOrDefault(c => c.CanWork(pendingTipe)) is ISqlTranslation conversor)
                    return conversor;

                if (_native.CanWork(pendingTipe))
                    return _native;

                return null;
            });
        }

        /// <summary>
        /// Gets a valid type for the expected type, handling nullable types.
        /// </summary>
        /// <param name="expectedType">The expected type.</param>
        /// <returns>The valid type.</returns>
        public static Type GetValidTypeFor(Type expectedType)
        {
            if (expectedType == null)
                return null;

            if (Nullable.GetUnderlyingType(expectedType) is Type underlyingType)
                return underlyingType;

            return expectedType;
        }

        /// <summary>
        /// Retrieves a value converter for a MemberInfo.
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public ISqlTranslation GetOf(MemberInfo member)
        {
            if (member.GetCustomAttribute<SqlConverterAttribute>() is SqlConverterAttribute attribute)
                return (ISqlTranslation)Activator.CreateInstance(attribute.Type);

            return null;
        }

        internal TableInfo AddTableMap<T>(TableMap<T> map)
        {
            var type = typeof(T);
            if (_mappedTables.ContainsKey(type))
                throw new InvalidOperationException(Messages.Table.AlreadyMapped);

            if (string.IsNullOrEmpty(map.Name))
                throw new ArgumentNullException(typeof(TableMap<T>).FullName + "." + nameof(TableMap<T>.Name));

            var table = new TableInfo(type, map.Registry, map.Name, map.softDelete, map.timestamp, map.GetFields());
            _mappedTables.TryAdd(type, table);
            return table;
        }

        /// <summary>
        /// Retrieves the name of the table.
        /// </summary>
        /// <param name="type">The type that should be used to retrieve the name.</param>
        /// <returns></returns>
        public string GetTableName(Type type)
        {
            return GetTable(type)?.Name;
        }

        /// <summary>
        /// Retrieves an instance of TableInfo.
        /// </summary>
        /// <param name="type">Type that should be represented by TableInfo.</param>
        /// <returns></returns>
        public TableInfo GetTable(Type type)
        {
            if (type == typeof(Row))
                return null;

            return _mappedTables.GetOrAdd(type, (pendingType) =>
            {
                return new TableInfo(type, this);
            });
        }

        #region IEquatable

        public override bool Equals(object obj)
        {
            return Equals(obj as TranslationRegistry);
        }

        public bool Equals(TranslationRegistry other)
        {
            return !(other is null) &&
                   EqualityComparer<NativeSqlTranslation>.Default.Equals(_native, other._native) &&
                   EqualityComparer<ISqlTranslation[]>.Default.Equals(Translators, other.Translators) &&
                   GuidFormat == other.GuidFormat &&
                   EqualityComparer<TimeZoneInfo>.Default.Equals(DbTimeZone, other.DbTimeZone) &&
                   EqualityComparer<TimeZoneInfo>.Default.Equals(TimeZone, other.TimeZone);
        }

        public override int GetHashCode()
        {
            int hashCode = 836003443;
            hashCode = hashCode * -1521134295 + EqualityComparer<NativeSqlTranslation>.Default.GetHashCode(_native);
            hashCode = hashCode * -1521134295 + EqualityComparer<ISqlTranslation[]>.Default.GetHashCode(Translators);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(GuidFormat);
            hashCode = hashCode * -1521134295 + EqualityComparer<TimeZoneInfo>.Default.GetHashCode(DbTimeZone);
            hashCode = hashCode * -1521134295 + EqualityComparer<TimeZoneInfo>.Default.GetHashCode(TimeZone);
            return hashCode;
        }

        public static bool operator ==(TranslationRegistry left, TranslationRegistry right)
        {
            return EqualityComparer<TranslationRegistry>.Default.Equals(left, right);
        }

        public static bool operator !=(TranslationRegistry left, TranslationRegistry right)
        {
            return !(left == right);
        }

        #endregion

        object ICloneable.Clone() => Clone();

        public TranslationRegistry Clone()
        {
            return new TranslationRegistry
            {
                DbTimeZone = DbTimeZone,
                GuidFormat = GuidFormat,
                TimeZone = TimeZone,
                Translators = Translators,
                EnumSerialization = EnumSerialization
            };
        }
    }
}
