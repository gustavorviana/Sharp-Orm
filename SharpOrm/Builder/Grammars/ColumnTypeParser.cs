using System;

namespace SharpOrm.Builder.Grammars
{
    /// <summary>
    /// Represents the parsed components of a database column type string.
    /// </summary>
    internal struct TypeInfo
    {
        public string DataType;
        public int? MaxLength;
        public int? Precision;
        public int? Scale;

        public TypeInfo(string dataType, int? maxLength, int? precision, int? scale)
        {
            DataType = dataType;
            MaxLength = maxLength;
            Precision = precision;
            Scale = scale;
        }
    }

    /// <summary>
    /// Utility class for parsing database column type strings.
    /// </summary>
    internal static class ColumnTypeParser
    {
        /// <summary>
        /// Parses a database type string (e.g., "int(11)", "varchar(100)", "decimal(10,2)") into components.
        /// </summary>
        /// <param name="typeString">The type string to parse.</param>
        /// <returns>A <see cref="TypeInfo"/> struct containing the parsed components.</returns>
        public static TypeInfo Parse(string typeString)
        {
            if (string.IsNullOrEmpty(typeString))
                return new TypeInfo(typeString, null, null, null);

            // Remove unsigned, zerofill, etc.
            int spaceIndex = typeString.IndexOf(' ');
            if (spaceIndex > 0)
                typeString = typeString.Substring(0, spaceIndex);

            int parenIndex = typeString.IndexOf('(');
            if (parenIndex < 0)
                return new TypeInfo(typeString, null, null, null);

            string dataType = typeString.Substring(0, parenIndex);
            int closeParenIndex = typeString.IndexOf(')');
            if (closeParenIndex <= parenIndex)
                return new TypeInfo(dataType, null, null, null);

            string sizeInfo = typeString.Substring(parenIndex + 1, closeParenIndex - parenIndex - 1);

            // Check if it's decimal/numeric with precision and scale
            if (sizeInfo.IndexOf(',') >= 0)
            {
                var parts = sizeInfo.Split(',');
                if (parts.Length == 2 && int.TryParse(parts[0].Trim(), out int p) && int.TryParse(parts[1].Trim(), out int s))
                {
                    return new TypeInfo(dataType, null, p, s);
                }
            }

            // Single number - could be maxLength or precision depending on type
            if (int.TryParse(sizeInfo, out int size))
            {
                // For numeric types, it's precision; for string/binary types, it's maxLength
                if (IsNumericType(dataType))
                    return new TypeInfo(dataType, null, size, null);

                return new TypeInfo(dataType, size, null, null);
            }

            return new TypeInfo(dataType, null, null, null);
        }

        /// <summary>
        /// Determines if a data type is a numeric type (decimal/numeric).
        /// </summary>
        private static bool IsNumericType(string dataType)
        {
            return dataType.Equals("decimal", StringComparison.OrdinalIgnoreCase) ||
                   dataType.Equals("numeric", StringComparison.OrdinalIgnoreCase);
        }
    }
}
