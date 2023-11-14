using SharpOrm.Builder.DataTranslation;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace SharpOrm.Builder
{
    internal class ColumnExpressionVisitor : ExpressionVisitor
    {
        private readonly QueryInfo _queryInfo;
        private readonly object _lock = new object();
        private readonly TranslationRegistry _registry = new TranslationRegistry();
        private readonly List<LambdaColumn> _columns = new List<LambdaColumn>();

        public ColumnExpressionVisitor(QueryInfo queryInfo)
        {
            this._queryInfo = queryInfo;
        }

        public override Expression Visit(Expression node)
        {
            lock (this._lock)
                return base.Visit(node);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (!(node.Member.MemberType == MemberTypes.Property || node.Member.MemberType == MemberTypes.Field))
                return base.VisitMember(node);

            this._columns.Insert(0, new LambdaColumn(node.Member));

            return base.VisitMember(node);
        }

        public IEnumerable<LambdaColumn> VisitColumn<T>(Expression<ColumnExpression<T>> check)
        {
            this._columns.Clear();
            this.Visit(check);
            return this._columns;
        }
    }
}
