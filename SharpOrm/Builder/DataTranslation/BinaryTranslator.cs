using System;
using System.IO;

namespace SharpOrm.Builder.DataTranslation
{
    /// <summary>
    /// Provides translation between binary data and SQL values.
    /// </summary>
    public class BinaryTranslator : ISqlTranslation
    {
        /// <summary>
        /// Determines whether the translator can work with the specified type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns><c>true</c> if the translator can work with the type; otherwise, <c>false</c>.</returns>
        public virtual bool CanWork(Type type) => type == typeof(byte[]) || type == typeof(MemoryStream);

        /// <summary>
        /// Checks if two types are considered the same for specific cases related to byte arrays and streams.
        /// </summary>
        /// <param name="type1">The first Type to compare.</param>
        /// <param name="type2">The second Type to compare.</param>
        /// <returns>True if the types are considered the same, otherwise false.</returns>
        internal static bool IsSame(Type type1, Type type2)
        {
            return (type1 == typeof(byte[]) && type2 == typeof(MemoryStream)) ||
                    (type2 == typeof(byte[]) && type1 == typeof(MemoryStream));
        }

        public virtual object FromSqlValue(object value, Type expectedType)
        {
            if (expectedType == typeof(byte[]))
                return this.ParseBytes(value);

            return this.ParseStream(value);
        }

        /// <summary>
        /// Parses the SQL value as a memory stream.
        /// </summary>
        /// <param name="value">The SQL value.</param>
        /// <returns>The parsed memory stream.</returns>
        public virtual MemoryStream ParseStream(object value)
        {
            if (value is byte[] buffer)
                return new MemoryStream(buffer);

            if (!(value is MemoryStream stream))
                throw new NotSupportedException("Invalid value provided. Expected a MemoryStream or byte[].");

            var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms;
        }

        public virtual object ToSqlValue(object value, Type type)
        {
            return this.ParseBytes(value);
        }

        /// <summary>
        /// Parses the value as an array of bytes.
        /// </summary>
        /// <param name="value">The value to parse.</param>
        /// <returns>The parsed array of bytes.</returns>
        public virtual byte[] ParseBytes(object value)
        {
            if (value is byte[] buffer)
                return buffer;

            if (value is MemoryStream ms)
                return ms.ToArray();

            if (!(value is MemoryStream stream) || !stream.CanRead)
                throw new NotSupportedException("Invalid value provided. Expected a MemoryStream or byte[].");

            using (ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                return ms.ToArray();
            }
        }
    }
}
