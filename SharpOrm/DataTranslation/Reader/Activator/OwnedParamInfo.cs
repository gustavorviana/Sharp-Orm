using SharpOrm.DataTranslation.Reader.NameResolvers;
using System;
using System.Data;
using System.Reflection;

namespace SharpOrm.DataTranslation.Reader.Activator
{
    internal class OwnedParamInfo : IParamInfo, IReaderInfo
    {
        private readonly ObjectRecordReader.ReaderObject _reader;

        public IDataRecord Record { get; }
        public TranslationRegistry Registry { get; }
        public ForeignInfo ForeignInfo { get; }

        public string Name { get; }

        public OwnedParamInfo(ParameterInfo parameter, IDataRecord reader, TranslationRegistry registry, Type type, string prefix)
        {
            Record = reader;
            Registry = registry;
            Name = parameter.Name;

            var table = registry.GetTable(type);
            _reader = new ObjectRecordReader.ReaderObject(this, null, type, table.Columns, new PrefixedColumnNameResolver(prefix));
        }

        public object GetValue()
        {
            return _reader.Read();
        }
    }
}
