using System;
using System.Data;

namespace SharpOrm.Builder.Grammars.Table
{
    /// <summary>
    /// Column type map for decimal type.
    /// Generates DECIMAL type with precision and scale support.
    /// </summary>
    public class DecimalColumnType : IColumnTypeMap
    {
        private readonly int _defaultPrecision;
        private readonly int _defaultScale;

        /// <summary>
        /// Initializes a new instance of the <see cref="DecimalColumnType"/> class.
        /// </summary>
        /// <param name="defaultPrecision">Default precision if not specified. Default is 18.</param>
        /// <param name="defaultScale">Default scale if not specified. Default is 2.</param>
        public DecimalColumnType(int defaultPrecision = 18, int defaultScale = 2)
        {
            _defaultPrecision = defaultPrecision;
            _defaultScale = defaultScale;
        }

        /// <summary>
        /// Determines if this type map can work with the specified type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if the type is decimal; otherwise false.</returns>
        public bool CanWork(Type type)
        {
            return type == typeof(decimal);
        }

        /// <summary>
        /// Builds the SQL type string for the column.
        /// </summary>
        /// <param name="column">The data column containing type metadata.</param>
        /// <returns>A SQL type string in the format DECIMAL(precision, scale).</returns>
        public string Build(DataColumn column)
        {
            int precision = _defaultPrecision;
            int scale = _defaultScale;

            if (column.ExtendedProperties.ContainsKey(ExtendedPropertyKeys.Precision))
                precision = (int)column.ExtendedProperties[ExtendedPropertyKeys.Precision];

            if (column.ExtendedProperties.ContainsKey(ExtendedPropertyKeys.Scale))
                scale = (int)column.ExtendedProperties[ExtendedPropertyKeys.Scale];

            return $"DECIMAL({precision},{scale})";
        }
    }
}
