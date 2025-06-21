using SharpOrm.DataTranslation;

namespace SharpOrm.ForeignKey
{
    internal class Includable<TEntity, TProperty> : IIncludable<TEntity, TProperty>, IIncludable
    {
        public ForeignKeyRegister Register { get; }
        public ForeignKeyNodeBase Node { get; }

        public Includable(ForeignKeyRegister register, ForeignKeyNodeBase node)
        {
            Register = register;
            Node = node;
        }
    }
}
