using System.Data.Common;

namespace SharpOrm.Builder.DataTranslation
{
    public interface IObjectTranslator
    {
        T ParseFromReader<T>(DbDataReader reader) where T : new();

        Row ToRow(object obj);
    }
}
