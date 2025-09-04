using System.Data;

namespace SharpOrm.DataTranslation.Reader
{
    internal class RowRecordReader : BaseRecordReader
    {
        public RowRecordReader(IDataReader reader, TranslationRegistry registry) : base(reader, registry)
        {
        }

        protected override object OnRead() => Reader.ReadRow(Registry);
    }
}
