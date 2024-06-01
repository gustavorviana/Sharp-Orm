using System.Data.Common;

namespace SharpOrm.DataTranslation.Reader
{
    public interface IMappedObject
    {
        object Read(DbDataReader reader);
    }
}
