using SharpOrm.DataTranslation;

namespace SharpOrm.ForeignKey
{
    internal class Includable<TEntity, TProperty> : IIncludable<TEntity, TProperty>
    {
        internal readonly ForeignKeyRegister _register;
        internal readonly ForeignKeyNodeBase _node;

        public Includable(ForeignKeyRegister register, ForeignKeyNodeBase node)
        {
            _register = register;
            _node = node;
        }
    }
}
