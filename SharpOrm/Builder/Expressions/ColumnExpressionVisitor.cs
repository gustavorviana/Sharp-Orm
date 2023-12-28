using SharpOrm.Builder.DataTranslation;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace SharpOrm.Builder.Expressions
{
    internal class ColumnExpressionVisitor : ExpressionVisitor
    {
        private readonly List<LambdaColumn> _columns = new List<LambdaColumn>();
        private readonly object _lock = new object();

        public override Expression Visit(Expression node)
        {
            lock (_lock)
                return base.Visit(node);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (!(node.Member.MemberType == MemberTypes.Property || node.Member.MemberType == MemberTypes.Field))
                return base.VisitMember(node);

            Validate(node);
            _columns.Insert(0, new LambdaColumn(node.Member));

            return base.VisitMember(node);
        }

        private static void Validate(MemberExpression node)
        {
            if (!TranslationUtils.IsNative(node.Type, false))
                return;

            string mType = node.Member.MemberType == MemberTypes.Property ? "property" : "field";
            throw new InvalidOperationException($"It's not possible to load the {mType} '{node.Member.Name}' because its type is incompatible.");
        }

        public IEnumerable<LambdaColumn> VisitColumn<T>(Expression<ColumnExpression<T>> check)
        {
            _columns.Clear();
            Visit(check);
            return _columns;
        }
    }
}
