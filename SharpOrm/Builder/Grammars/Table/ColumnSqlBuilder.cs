using System;
using System.Data;

namespace SharpOrm.Builder.Grammars.Table
{
    /// <summary>
    /// Base class for building SQL column definitions and extracting column metadata from <see cref="DataColumn"/>.
    /// </summary>
    public abstract class ColumnSqlBuilder
    {
        /// <summary>
        /// Gets the <see cref="DataColumn"/> associated with this builder.
        /// </summary>
        protected DataColumn Column { get; }

        /// <summary>
        /// Gets the <see cref="QueryConfig"/> used for SQL translation and configuration.
        /// </summary>
        protected QueryConfig Config { get; }

        private readonly string _type;

        /// <summary>
        /// Gets a value indicating whether the column is computed.
        /// </summary>
        protected bool IsComputed => HasProperty(ExtendedPropertyKeys.ComputedExpression);

        /// <summary>
        /// Initializes a new instance of the <see cref="ColumnSqlBuilder"/> class.
        /// </summary>
        /// <param name="config">The query configuration.</param>
        /// <param name="type">The SQL type of the column.</param>
        /// <param name="column">The <see cref="DataColumn"/> to build from.</param>
        protected ColumnSqlBuilder(QueryConfig config, string type, DataColumn column)
        {
            _type = type;
            Column = column;
            Config = config;
        }

        /// <summary>
        /// Builds the SQL expression for the column definition.
        /// </summary>
        /// <returns>The <see cref="SqlExpression"/> representing the column.</returns>
        public abstract SqlExpression Build();

        /// <summary>
        /// Creates a new <see cref="QueryBuilder"/> for building SQL fragments.
        /// </summary>
        /// <returns>A configured <see cref="QueryBuilder"/> instance.</returns>
        protected QueryBuilder GetBuilder()
        {
            return new QueryBuilder(Config, new DbName())
            {
                NoParameters = true,
                paramInterceptor = OnInterceptParam
            };
        }

        /// <summary>
        /// Intercepts and converts parameter values for SQL, especially for date/time types.
        /// </summary>
        /// <param name="value">The value to intercept.</param>
        /// <returns>The converted value for SQL.</returns>
        protected virtual object OnInterceptParam(object value)
        {
            if (value == null)
                return null;

            if (Config.Translation.IsDateOrTime(value.GetType()))
                return Config.Translation.ToSql(value, typeof(string));

            return value;
        }

        #region ExtendedProperties Accessors

        /// <summary>
        /// Checks if the column has a specific extended property.
        /// </summary>
        /// <param name="property">The property key.</param>
        /// <returns>True if the property exists; otherwise, false.</returns>
        protected bool HasProperty(string property)
        {
            return Column.ExtendedProperties.ContainsKey(property);
        }

        /// <summary>
        /// Gets a boolean value from the column's extended properties.
        /// </summary>
        /// <param name="key">The property key.</param>
        /// <param name="defaultValue">The default value if the property is not found.</param>
        /// <returns>The boolean value.</returns>
        protected bool GetBoolean(string key, bool defaultValue = false)
        {
            if (!HasProperty(key))
                return defaultValue;

            var value = Column.ExtendedProperties[key];
            return value is bool boolValue ? boolValue : bool.TryParse(value?.ToString(), out var result) && result;
        }

        /// <summary>
        /// Gets an integer value from the column's extended properties.
        /// </summary>
        /// <param name="key">The property key.</param>
        /// <param name="defaultValue">The default value if the property is not found.</param>
        /// <returns>The integer value.</returns>
        protected int GetInt(string key, int defaultValue = 0)
        {
            if (!HasProperty(key))
                return defaultValue;

            var value = Column.ExtendedProperties[key];
            return value is int intValue ? intValue : int.TryParse(value?.ToString(), out var result) ? result : defaultValue;
        }

        /// <summary>
        /// Gets a long value from the column's extended properties.
        /// </summary>
        /// <param name="key">The property key.</param>
        /// <param name="defaultValue">The default value if the property is not found.</param>
        /// <returns>The long value.</returns>
        protected long GetLong(string key, long defaultValue = 0L)
        {
            if (!HasProperty(key))
                return defaultValue;

            var value = Column.ExtendedProperties[key];
            return value is long longValue ? longValue : long.TryParse(value?.ToString(), out var result) ? result : defaultValue;
        }

        /// <summary>
        /// Gets a string value from the column's extended properties.
        /// </summary>
        /// <param name="key">The property key.</param>
        /// <returns>The string value, or null if not found.</returns>
        protected string GetString(string key)
        {
            return HasProperty(key)
                ? Column.ExtendedProperties[key]?.ToString()
                : null;
        }

        /// <summary>
        /// Gets a value indicating whether the column is a primary key.
        /// </summary>
        protected bool GetIsPrimaryKey() => GetBoolean(ExtendedPropertyKeys.IsPrimaryKey);

        /// <summary>
        /// Gets a value indicating whether the column is unique.
        /// </summary>
        protected bool GetIsUnique() => GetBoolean(ExtendedPropertyKeys.IsUnique);

        /// <summary>
        /// Gets the default value for the column.
        /// </summary>
        protected string GetDefaultValue() => GetString(ExtendedPropertyKeys.DefaultValue);

        /// <summary>
        /// Gets the check constraint for the column.
        /// </summary>
        protected string GetCheckConstraint() => GetString(ExtendedPropertyKeys.CheckConstraint);

        /// <summary>
        /// Gets a value indicating whether the column has a default value.
        /// </summary>
        protected bool HasDefaultValue => HasProperty(ExtendedPropertyKeys.DefaultValue);

        /// <summary>
        /// Gets the precision for numeric columns.
        /// </summary>
        protected int GetPrecision() => GetInt(ExtendedPropertyKeys.Precision);

        /// <summary>
        /// Gets the scale for numeric columns.
        /// </summary>
        protected int GetScale() => GetInt(ExtendedPropertyKeys.Scale);

        /// <summary>
        /// Gets the maximum length for string columns.
        /// </summary>
        protected int GetMaxLength() => GetInt(ExtendedPropertyKeys.MaxLength);

        /// <summary>
        /// Gets the SQL type of the column.
        /// </summary>
        protected string GetColumnType() => _type;

        /// <summary>
        /// Gets the foreign table name for the column.
        /// </summary>
        protected string GetForeignTable() => GetString(ExtendedPropertyKeys.ForeignTable);

        /// <summary>
        /// Gets the referenced table name for the foreign key.
        /// </summary>
        protected string GetReferencedTable() => GetString(ExtendedPropertyKeys.ReferencedTable);

        /// <summary>
        /// Gets the referenced column name for the foreign key.
        /// </summary>
        protected string GetReferencedColumn() => GetString(ExtendedPropertyKeys.ReferencedColumn);

        /// <summary>
        /// Gets the ON DELETE action for the foreign key.
        /// </summary>
        protected string GetOnDelete() => GetString(ExtendedPropertyKeys.OnDelete);

        /// <summary>
        /// Gets the ON UPDATE action for the foreign key.
        /// </summary>
        protected string GetOnUpdate() => GetString(ExtendedPropertyKeys.OnUpdate);

        /// <summary>
        /// Gets the foreign key constraint name.
        /// </summary>
        protected string GetForeignKeyName() => GetString(ExtendedPropertyKeys.ForeignKeyName);

        /// <summary>
        /// Gets the computed expression for the column.
        /// </summary>
        protected string GetComputedExpression() => GetString(ExtendedPropertyKeys.ComputedExpression);

        /// <summary>
        /// Gets a value indicating whether the column is virtual.
        /// </summary>
        protected bool GetIsVirtual() => GetBoolean(ExtendedPropertyKeys.IsVirtual);

        /// <summary>
        /// Gets the identity seed value for the column.
        /// </summary>
        protected long GetIdentitySeed() => GetLong(ExtendedPropertyKeys.IdentitySeed, 1L);

        /// <summary>
        /// Gets the identity increment value for the column.
        /// </summary>
        protected long GetIdentityIncrement() => GetLong(ExtendedPropertyKeys.IdentityIncrement, 1L);

        /// <summary>
        /// Gets the comment for the column.
        /// </summary>
        protected string GetComment() => GetString(ExtendedPropertyKeys.Comment);

        /// <summary>
        /// Gets the description for the column.
        /// </summary>
        protected string GetDescription() => GetString(ExtendedPropertyKeys.Description);

        /// <summary>
        /// Gets the collation for the column.
        /// </summary>
        protected string GetCollation() => GetString(ExtendedPropertyKeys.Collation);

        /// <summary>
        /// Gets the position of the column in the table.
        /// </summary>
        protected int GetPosition() => GetInt(ExtendedPropertyKeys.Position);

        #endregion
    }
}