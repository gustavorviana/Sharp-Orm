using SharpOrm.Builder;
using SharpOrm.DataTranslation;
using System;

namespace SharpOrm.ForeignKey
{
    public interface IIncludable<out TEntity, out TProperty> : IIncludable<TEntity>
    {
    }

    public interface IIncludable<out TEntity>
    {
        [Obsolete("This Property will be removed in version 4.0", true)]
        DbName Name { get; }
    }

    internal interface IIncludable
    {
        ForeignKeyRegister Register { get; }
        ForeignKeyNodeBase Node { get; }
    }
}
