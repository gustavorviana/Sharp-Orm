using SharpOrm.Builder;
using SharpOrm.DataTranslation;

namespace SharpOrm.ForeignKey
{
    internal class Includable<TEntity, TProperty> : IIncludable<TEntity, TProperty>, IIncludable
    {
        public DbName Name => Node.Name;

        public ForeignKeyRegister Register { get; }
        public ForeignKeyNodeBase Node { get; }

        public Includable(ForeignKeyRegister register, ForeignKeyNodeBase node)
        {
            Register = register;
            Node = node;
        }
    }
}
