using SharpOrm.Builder;

namespace SharpOrm.DataTranslation
{
    public interface IObjectReaderFactory
    {
        ObjectReaderBase OfType(TableInfo table);
    }

    public class ObjectReaderFactory : IObjectReaderFactory
    {
        public ObjectReaderBase OfType(TableInfo table)
        {
            if (ReflectionUtils.IsDynamic(table.Type))
                return new DynamicObjectReader(table);

            return new ObjectReader(table);
        }
    }
}
