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

        /// <summary>
        /// Gets or sets the default translation registry instance used throughout the application.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when attempting to set a null value.</exception>
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
            get => _native._dateTranslation.Format;
            set => _native._dateTranslation.Format = value;
        }

        /// <summary>
        /// Gets or sets the culture used for numeric translations.
        /// </summary>
        public CultureInfo Culture
        {
            get => _native._numericTranslation.Culture;
            set => _native._numericTranslation.Culture = value;
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
        /// Gets or sets the behavior when an invalid value is encountered during Guid or Enum conversion.
        /// </summary>
        /// <value>Default is <see cref="InvalidValueBehavior.ThrowException"/>.</value>
        public InvalidValueBehavior InvalidValueBehavior
        {
            get => _native.InvalidValueBehavior;
            set => _native.InvalidValueBehavior = value;
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
        /// Gets or sets the mode for mapping nested objects.
        /// </summary>
        public NestedMode NestedMapMode { get; set; } = NestedMode.Attribute;

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


        /// <summary>
        /// Determines whether the specified type represents a date or time type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns><c>true</c> if the type is a date or time type; otherwise, <c>false</c>.</returns>
        public bool IsDateOrTime(Type type)
        {
            return _native._dateTranslation.CanWork(type);
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

                if (type.GetCustomAttribute<SqlConverterAttribute>() is SqlConverterAttribute attribute)
                    return (ISqlTranslation)Activator.CreateInstance(attribute.Type);

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
            return TranslationUtils.GetValidTypeFor(expectedType, out _);
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

            if (ReflectionUtils.GetMemberType(member).GetCustomAttribute<SqlConverterAttribute>() is SqlConverterAttribute typeAttribute)
                return (ISqlTranslation)Activator.CreateInstance(typeAttribute.Type);

            return null;
        }

        internal TableInfo AddTableMap<T>(ModelMapper<T> map)
        {
            var type = typeof(T);
            if (_mappedTables.ContainsKey(type))
                throw new InvalidOperationException(Messages.Table.AlreadyMapped);

            if (string.IsNullOrEmpty(map.Name))
                throw new ArgumentNullException(typeof(ModelMapper<T>).FullName + "." + nameof(ModelMapper<T>.Name));

            var table = new TableInfo(map);
            _mappedTables.TryAdd(type, table);
            return table;
        }

        /// <summary>
        /// Retrieves the name of the table associated with the specified generic type.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <returns>The table name, or <c>null</c> if no table is found.</returns>
        public string GetTableName<T>()
        {
            return GetTable(typeof(T))?.Name;
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
        /// Retrieves the table information for the specified generic type.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <returns>The <see cref="TableInfo"/> instance representing the table.</returns>
        public TableInfo GetTable<T>()
        {
            return GetTable(typeof(T));
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

        /// <summary>
        /// Determines whether the specified object is equal to the current <see cref="TranslationRegistry"/>.
        /// </summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns><c>true</c> if the specified object is equal to the current instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as TranslationRegistry);
        }

        /// <summary>
        /// Determines whether the specified <see cref="TranslationRegistry"/> is equal to the current instance.
        /// </summary>
        /// <param name="other">The <see cref="TranslationRegistry"/> to compare with the current instance.</param>
        /// <returns><c>true</c> if the specified instance is equal to the current instance; otherwise, <c>false</c>.</returns>
        public bool Equals(TranslationRegistry other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return _native.Equals(other._native) &&
                   DotnetUtils.SequenceEqual(Translators, other.Translators) &&
                   GuidFormat == other.GuidFormat &&
                   EqualityComparer<TimeZoneInfo>.Default.Equals(DbTimeZone, other.DbTimeZone) &&
                   EqualityComparer<TimeZoneInfo>.Default.Equals(TimeZone, other.TimeZone) &&
                   EnumSerialization == other.EnumSerialization &&
                   Culture.Equals(other.Culture) &&
                   DateFormat == other.DateFormat &&
                   EmptyStringToNull == other.EmptyStringToNull;
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            int hashCode = 836003443;
            hashCode = hashCode * -1521134295 + GetTranslatorsHashCode();
            hashCode = hashCode * -1521134295 + (GuidFormat?.GetHashCode() ?? 0);
            hashCode = hashCode * -1521134295 + (DbTimeZone?.GetHashCode() ?? 0);
            hashCode = hashCode * -1521134295 + (TimeZone?.GetHashCode() ?? 0);
            hashCode = hashCode * -1521134295 + EnumSerialization.GetHashCode();
            hashCode = hashCode * -1521134295 + (Culture?.GetHashCode() ?? 0);
            hashCode = hashCode * -1521134295 + (DateFormat?.GetHashCode() ?? 0);
            hashCode = hashCode * -1521134295 + EmptyStringToNull.GetHashCode();
            return hashCode;
        }

        private int GetTranslatorsHashCode()
        {
            if (Translators == null)
                return 0;

            unchecked
            {
                int hash = 836003443;
                foreach (var translator in Translators)
                    hash = hash * -1521134295 + (translator?.GetType().GetHashCode() ?? 0);
                return hash;
            }
        }

        /// <summary>
        /// Determines whether two <see cref="TranslationRegistry"/> instances are equal.
        /// </summary>
        /// <param name="left">The first instance to compare.</param>
        /// <param name="right">The second instance to compare.</param>
        /// <returns><c>true</c> if the instances are equal; otherwise, <c>false</c>.</returns>
        public static bool operator ==(TranslationRegistry left, TranslationRegistry right)
        {
            return EqualityComparer<TranslationRegistry>.Default.Equals(left, right);
        }

        /// <summary>
        /// Determines whether two <see cref="TranslationRegistry"/> instances are not equal.
        /// </summary>
        /// <param name="left">The first instance to compare.</param>
        /// <param name="right">The second instance to compare.</param>
        /// <returns><c>true</c> if the instances are not equal; otherwise, <c>false</c>.</returns>
        public static bool operator !=(TranslationRegistry left, TranslationRegistry right)
        {
            return !(left == right);
        }

        #endregion

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        object ICloneable.Clone() => Clone();

        /// <summary>
        /// Creates a deep copy of the current <see cref="TranslationRegistry"/> instance.
        /// </summary>
        /// <returns>A new <see cref="TranslationRegistry"/> instance with the same configuration.</returns>
        public TranslationRegistry Clone()
        {
            var clone = new TranslationRegistry();
            var type = typeof(TranslationRegistry);

            ReflectionUtils.CloneFields(this, clone, "_native");
            ReflectionUtils.CloneProperties(this, clone);

            return clone;
        }
    }
}
