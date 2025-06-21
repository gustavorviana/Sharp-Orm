using SharpOrm.Builder;
using SharpOrm.DataTranslation;

namespace SharpOrm.ForeignKey
{
    public interface IIncludable<out TEntity, out TProperty>
    {
        DbName Name { get; }
    }

    internal interface IIncludable
    {
        DbName Name { get; }
        ForeignKeyRegister Register { get; }
        ForeignKeyNodeBase Node { get; }
    }
}
