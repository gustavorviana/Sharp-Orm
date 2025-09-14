using SharpOrm.Builder;
using System;
using System.Data;

namespace SharpOrm.DataTranslation.Reader
{
    public interface IRecordReaderFactory
    {
        BaseRecordReader OfType(Type type, IDataReader reader, TranslationRegistry registry);
    }

    public class RecordReaderFactory : IRecordReaderFactory
    {
        internal ForeignInfo ForeignInfo { get; set; }

        public BaseRecordReader OfType(Type type, IDataReader reader, TranslationRegistry registry)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            if (type == typeof(Row))
                return new RowRecordReader(reader, registry);

            if (ReflectionUtils.IsDynamic(type))
                return new DynamicRecordReader(reader, registry);

            return new ObjectRecordReader(ForeignInfo, registry.GetTable(type), reader);
        }
    }
}
