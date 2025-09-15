using System;
using System.Data;

namespace SharpOrm.DataTranslation.Reader
{
    internal class ValueRecordReader : BaseRecordReader
    {
        private readonly ISqlTranslation _translation;
        private readonly Type _type;

        public ValueRecordReader(Type valueType, ISqlTranslation translation, IDataReader reader, TranslationRegistry registry) : base(reader, registry)
        {
            _translation = translation;
            _type = valueType;
        }

        protected override object OnRead() => _translation.FromSqlValue(Reader[0], _type);
    }
}
