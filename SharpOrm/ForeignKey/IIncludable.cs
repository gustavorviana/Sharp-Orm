namespace SharpOrm.ForeignKey
{
    public interface IIncludable<out TEntity, out TProperty> : IIncludable
    {
    }

    public interface IIncludable { }
}
