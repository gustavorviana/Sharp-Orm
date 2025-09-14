using SharpOrm.Builder;
using SharpOrm.DataTranslation;
using System;

namespace SharpOrm.ForeignKey
{
    internal class Includable<TEntity, TProperty> : IIncludable<TEntity, TProperty>, IIncludable
    {
        [Obsolete("This Property will be removed in version 4.0", true)]
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
