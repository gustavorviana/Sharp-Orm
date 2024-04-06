using System.Data.Common;

namespace SharpOrm.Builder.DataTranslation.Reader
{
    public interface IMappedObject
    {
        object Read(DbDataReader reader);
    }
}
