using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SharpOrm.Builder.DataTranslation
{
    public class BinaryTranslator : ISqlTranslation
    {
        public virtual bool CanWork(Type type) => type != null && (type == typeof(byte[]) || type == typeof(Stream) || type.IsSubclassOf(typeof(Stream)));

        public virtual object FromSqlValue(object value, Type expectedType)
        {
            if (expectedType == typeof(byte[]))
                return this.ParseBytes(value);

            return this.ParseStream(value);
        }

        public virtual MemoryStream ParseStream(object value)
        {
            if (value is byte[] buffer)
                return new MemoryStream(buffer);

            if (!(value is Stream stream))
                throw new NotSupportedException();

            var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms;
        }

        public virtual object ToSqlValue(object value, Type type)
        {
            return this.ParseBytes(value);
        }

        public virtual byte[] ParseBytes(object value)
        {
            if (value is byte[] buffer)
                return buffer;

            if (value is MemoryStream ms)
                return ms.ToArray();

            if (!(value is Stream stream) || !stream.CanRead)
                throw new NotSupportedException();

            using (ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                return ms.ToArray();
            }
        }
    }
}
