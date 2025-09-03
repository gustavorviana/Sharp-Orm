using System.Data;

namespace SharpOrm.DataTranslation.Reader
{
    internal class MappedRowObject : IMappedObject
    {
        private readonly TranslationRegistry _registry;

        public MappedRowObject(TranslationRegistry registry)
        {
            _registry = registry;
        }

        public object Read(IDataRecord record)
            => record.ReadRow(_registry);
    }
}
