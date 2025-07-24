using SharpOrm.Builder;
using SharpOrm.DataTranslation;
using SharpOrm.ForeignKey;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace SharpOrm
{
    internal static class IncludeExtension
    {
        public static IIncludable<TEntity, TProperty> Include<TEntity, TProperty>(
        this IIncludable<TEntity, TProperty> source,
        Expression<Func<TEntity, TProperty>> navigationPropertyPath)
        where TEntity : class
        {
            var sourceClass = source as Includable<TEntity, TProperty>;

            return InternalInclude<TEntity, TProperty>(sourceClass.Register, navigationPropertyPath);
        }

        public static IIncludable<TEntity, TProperty> Include<TEntity, TPreviousProperty, TProperty>(
        this IIncludable<TEntity, IEnumerable<TPreviousProperty>> source,
        Expression<Func<TPreviousProperty, TProperty>> navigationPropertyPath)
        where TEntity : class
        {
            var sourceClass = source as IIncludable;

            return InternalInclude<TEntity, TProperty>(sourceClass.Register, navigationPropertyPath);
        }

        public static IIncludable<TEntity, TProperty> ThenInclude<TEntity, TPreviousProperty, TProperty>(
        this IIncludable<TEntity, IEnumerable<TPreviousProperty>> source,
        Expression<Func<TPreviousProperty, TProperty>> navigationPropertyPath)
        where TEntity : class
        {
            var sourceClass = source as IIncludable;

            return InternalInclude<TEntity, TProperty>(sourceClass.Node, navigationPropertyPath);
        }

        public static IIncludable<TEntity, TProperty> ThenInclude<TEntity, TPreviousProperty, TProperty>(
        this IIncludable<TEntity, TPreviousProperty> source,
        Expression<Func<TPreviousProperty, TProperty>> navigationPropertyPath)
        where TEntity : class
        {
            var sourceClass = source as IIncludable;

            return InternalInclude<TEntity, TProperty>(sourceClass.Node, navigationPropertyPath);
        }

        internal static IIncludable<TEntity, TProperty> InternalInclude<TEntity, TProperty>(ForeignKeyNodeBase parent, LambdaExpression expression)
        {
            var members = ExpressionUtils<TEntity>.GetMemberPath(expression, false).Reverse();

            foreach (var member in members)
                parent = parent.GetOrAddChild(member);

            return ((ForeignKeyNode)parent).GetIncludable<TEntity, TProperty>();
        }
    }
}
