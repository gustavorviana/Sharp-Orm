using SharpOrm.DataTranslation.Reader.NameLoader;
using System;
using System.Data;

namespace SharpOrm.DataTranslation.Reader.Activator
{
    internal class OwnedParamInfo : IParamInfo, IReaderInfo
    {
        private readonly ObjectRecordReader.ReaderObject _reader;

        public IDataRecord Record { get; }
        public TranslationRegistry Registry { get; }
        public ForeignInfo ForeignInfo { get; }

        public OwnedParamInfo(IDataRecord reader, TranslationRegistry registry, Type type, string prefix)
        {
            Record = reader;
            Registry = registry;

            var table = registry.GetTable(type);
            _reader = new ObjectRecordReader.ReaderObject(this, null, type, table.Columns, new WithPrefixColumnNameLoader(prefix));
        }

        public object GetValue()
        {
            return _reader.Read();
        }
    }
}
