using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace SharpOrm.Builder.Expressions
{
    internal class PropertyExpressionVisitor : ExpressionVisitor
    {
        private string propName = null;

        protected override Expression VisitMember(MemberExpression node)
        {
            if (!(node.Member.MemberType == MemberTypes.Property || node.Member.MemberType == MemberTypes.Field))
                return base.VisitMember(node);

            propName = node.Member.Name;

            return node;
        }

        public string VisitProperty<T>(Expression<ColumnExpression<T>> check)
        {
            propName = null;
            Visit(check);
            return propName;
        }

        public static IEnumerable<string> VisitProperties<T>(Expression<ColumnExpression<T>> call1, Expression<ColumnExpression<T>>[] calls)
        {
            var visitor = new PropertyExpressionVisitor();

            if (visitor.VisitProperty(call1) is string property1)
                yield return property1;

            foreach (var call in calls)
                if (visitor.VisitProperty(call) is string property)
                    yield return property;
        }
    }
}
