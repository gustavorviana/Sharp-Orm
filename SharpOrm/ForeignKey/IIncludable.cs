using SharpOrm.DataTranslation;

namespace SharpOrm.ForeignKey
{
    public interface IIncludable<out TEntity, out TProperty>
    {
    }

    internal interface IIncludable
    {
        ForeignKeyRegister Register { get; }
        ForeignKeyNodeBase Node { get; }
    }
}
