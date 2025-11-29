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
            this IIncludable<TEntity> source,
            Expression<Func<TEntity, TProperty>> navigationPropertyPath)
            where TEntity : class
        {
            if (!(source is IIncludable includable))
                throw new ArgumentException("Source must implement IIncludable interface", nameof(source));

            return InternalInclude<TEntity, TProperty>(includable.Register, navigationPropertyPath);
        }

        public static IIncludable<TEntity, TProperty> ThenInclude<TEntity, TPreviousProperty, TProperty>(
            this IIncludable<TEntity, IEnumerable<TPreviousProperty>> source,
            Expression<Func<TPreviousProperty, TProperty>> navigationPropertyPath)
            where TEntity : class
        {
            if (!(source is IIncludable includable))
                throw new ArgumentException("Source must implement IIncludable interface", nameof(source));

            return InternalInclude<TEntity, TProperty>(includable.Node, navigationPropertyPath);
        }

        public static IIncludable<TEntity, TProperty> ThenInclude<TEntity, TPreviousProperty, TProperty>(
            this IIncludable<TEntity, TPreviousProperty> source,
            Expression<Func<TPreviousProperty, TProperty>> navigationPropertyPath)
            where TEntity : class
        {
            if (!(source is IIncludable includable))
                throw new ArgumentException("Source must implement IIncludable interface", nameof(source));

            return InternalInclude<TEntity, TProperty>(includable.Node, navigationPropertyPath);
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
