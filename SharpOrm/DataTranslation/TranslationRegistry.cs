using SharpOrm.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SharpOrm.DataTranslation
{
    /// <summary>
    /// Class responsible for translating data between the database and code.
    /// </summary>
    public class TranslationRegistry : IEquatable<TranslationRegistry>, ICloneable
    {
        private readonly NativeSqlTranslation native = new NativeSqlTranslation();
        private static TranslationRegistry _default = new TranslationRegistry();
        private readonly List<TableInfo> _manualMapped = new List<TableInfo>();

        public static TranslationRegistry Default
        {
            get => _default;
            set => _default = value ?? throw new ArgumentNullException(nameof(Default));
        }

        /// <summary>
        /// Custom value translators.
        /// </summary>
        public ISqlTranslation[] Translators { get; set; } = new ISqlTranslation[0];

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
            get => native.GuidFormat;
            set => native.GuidFormat = value;
        }

        /// <summary>
        /// Gets or sets the serialization format for enums.
        /// </summary>
        /// <value>The serialization format for enums.</value>
        public EnumSerialization EnumSerialization
        {
            get => native.EnumSerialization;
            set => native.EnumSerialization = value;
        }

        /// <summary>
        /// Timezone in which dates should be stored in the database.
        /// </summary>
        /// <value><see cref="TimeZoneInfo.Local"/></value>
        public TimeZoneInfo DbTimeZone
        {
            get => native.DbTimeZone;
            set => native.DbTimeZone = value;
        }

        /// <summary>
        /// Timezone in which dates should be converted to work within the code.
        /// </summary>
        /// <value><see cref="TimeZoneInfo.Local"/></value>
        public TimeZoneInfo TimeZone
        {
            get => native.TimeZone;
            set => native.TimeZone = value;
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

            throw new NotSupportedException($"Type \"{expectedType.FullName}\" is not supported");
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

            if (Translators?.FirstOrDefault(c => c.CanWork(type)) is ISqlTranslation conversor)
                return conversor;

            if (native.CanWork(type))
                return native;

            return null;
        }

        /// <summary>
        /// Gets a valid type for the expected type, handling nullable types.
        /// </summary>
        /// <param name="expectedType">The expected type.</param>
        /// <returns>The valid type.</returns>
        public static Type GetValidTypeFor(Type expectedType)
        {
            if (expectedType != null && Nullable.GetUnderlyingType(expectedType) is Type underlyingType)
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
            if (_manualMapped.Any(x => x.Type == type))
                throw new InvalidOperationException("The type has already been mapped.");

            if (string.IsNullOrEmpty(map.Name))
                throw new ArgumentNullException("SharpOrm.Builder.TableMap<T>.Name");

            var table = new TableInfo(type, map.Registry, map.Name, map.softDelete, map.timestamp, map.GetFields());
            _manualMapped.Add(table);
            return table;
        }

        /// <summary>
        /// Retrieves the name of the table.
        /// </summary>
        /// <param name="type">The type that should be used to retrieve the name.</param>
        /// <returns></returns>
        public string GetTableName(Type type)
        {
            if (GetManualMap(type) is TableInfo table) return table.Name;
            return TableInfo.GetNameOf(type);
        }

        /// <summary>
        /// Retrieves an instance of TableInfo.
        /// </summary>
        /// <param name="type">Type that should be represented by TableInfo.</param>
        /// <returns></returns>
        public TableInfo GetTable(Type type)
        {
#pragma warning disable CS0618 // O tipo ou membro é obsoleto
            return GetManualMap(type) ?? new TableInfo(type, this);
#pragma warning restore CS0618 // O tipo ou membro é obsoleto
        }

        internal TableInfo GetManualMap(Type type)
        {
            return _manualMapped.FirstOrDefault(x => x.Type == type);
        }

        #region IEquatable

        public override bool Equals(object obj)
        {
            return Equals(obj as TranslationRegistry);
        }

        public bool Equals(TranslationRegistry other)
        {
            return !(other is null) &&
                   EqualityComparer<NativeSqlTranslation>.Default.Equals(native, other.native) &&
                   EqualityComparer<ISqlTranslation[]>.Default.Equals(Translators, other.Translators) &&
                   GuidFormat == other.GuidFormat &&
                   EqualityComparer<TimeZoneInfo>.Default.Equals(DbTimeZone, other.DbTimeZone) &&
                   EqualityComparer<TimeZoneInfo>.Default.Equals(TimeZone, other.TimeZone);
        }

        public override int GetHashCode()
        {
            int hashCode = 836003443;
            hashCode = hashCode * -1521134295 + EqualityComparer<NativeSqlTranslation>.Default.GetHashCode(native);
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
