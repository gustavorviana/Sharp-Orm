using SharpOrm.Msg;
using System;
using System.IO;

namespace SharpOrm.DataTranslation
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

        public virtual object FromSqlValue(object value, Type expectedType)
        {
            if (expectedType == typeof(byte[]))
                return ParseBytes(value);

            return ParseStream(value);
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
                throw new NotSupportedException(Messages.Translation.BytesExpected);

            var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms;
        }

        public virtual object ToSqlValue(object value, Type type)
        {
            return ParseBytes(value);
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
                throw new NotSupportedException(Messages.Translation.BytesExpected);

            using (ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                return ms.ToArray();
            }
        }
    }
}
