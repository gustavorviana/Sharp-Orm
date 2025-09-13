using System.Data;

namespace SharpOrm.Builder
{
    public interface IColumnTypeProvider
    {
        string BuildType(DataColumn column);
        IColumnTypeMap Get(DataColumn column);
    }
}
