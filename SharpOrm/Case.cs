using SharpOrm.Builder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SharpOrm
{
    public class Case : Column
    {
        private readonly List<CaseNode> nodes = new List<CaseNode>();
        private object elseValue;

        #region Constructor
        public Case()
        {
        }

        public Case(Column column) : base(column)
        {

        }

        public Case(DbName column) : base(column)
        {

        }

        public Case(string columnName) : base(columnName)
        {
        }

        public Case(string columnName, string alias) : base(columnName, alias)
        {
        }

        #endregion

        #region When

        public Case WhenNull(string column, object then)
        {
            this.nodes.Add(new CaseNode
            {
                Column = new Column(column),
                Expression = new SqlExpression($"IS NULL"),
                Then = then,
            });
            return this;
        }

        public Case WhenNotNull(string column, object then)
        {
            this.nodes.Add(new CaseNode
            {
                Column = new Column(column),
                Expression = new SqlExpression($"IS NOT NULL"),
                Then = then,
            });
            return this;
        }

        public Case When(string column, object value, object then)
        {
            return this.When(column, "=", value, then);
        }

        public Case When(SqlExpression expression, object then)
        {
            if (then is ICollection)
                throw new NotSupportedException();

            this.nodes.Add(new CaseNode { Expression = expression, Then = then });
            return this;
        }

        public Case When(string column, string operation, object value, object then)
        {
            QueryBase.CheckIsAvailableOperation(operation);
            if (then is ICollection)
                throw new NotSupportedException();

            this.nodes.Add(new CaseNode
            {
                Column = new Column(column),
                Expression = new SqlExpression($"{operation} ?", value),
                Then = then,
            });
            return this;
        }

        public Case When(object expected, object then)
        {
            this.nodes.Add(new CaseNode
            {
                Expression = expected is SqlExpression exp ? exp : new SqlExpression("?", expected),
                Then = then
            });
            return this;
        }
        #endregion

        public Column Else(object value)
        {
            if (value is ICollection)
                throw new NotSupportedException();

            this.elseValue = value == null ? DBNull.Value : value;
            return this;
        }

        public override SqlExpression ToExpression(IReadonlyQueryInfo info, bool alias)
        {
            if (this.nodes.Count == 0)
                throw new InvalidOperationException("You cannot use an empty case.");

            var query = new QueryConstructor(info);
            this.WriteCase(query, info);

            foreach (var node in this.nodes)
                node.WriteTo(query, info);

            if (this.elseValue != null)
                query.Add("ELSE ").AddParameter(this.elseValue).Add();

            query.Add("END");

            if (alias && !string.IsNullOrEmpty(this.Alias))
                query.AddFormat(" AS {0}", info.Config.ApplyNomenclature(this.Alias));

            return query.ToExpression(info);
        }

        private QueryConstructor WriteCase(QueryConstructor query, IReadonlyQueryInfo info)
        {
            query.Add("CASE");

            if (this.expression != null)
                return query.Add().Add(this.expression, false).Add();

            if (!string.IsNullOrEmpty(this.Name))
                return query.AddFormat(" {0} ", info.Config.ApplyNomenclature(this.Name));

            return query.Add();
        }

        private class CaseNode
        {
            public SqlExpression Expression;
            public Column Column;

            public object Then;

            public QueryConstructor WriteTo(QueryConstructor query, IReadonlyQueryInfo info)
            {
                query.Add("WHEN ");

                if (this.Column != null)
                    query.AddParameter(this.Column).Add();

                query.Add(this.Expression, false).Add(" THEN ");

                if (this.Then is SqlExpression exp)
                    return query.Add().AddParameter(exp).Add();

                return query.AddParameter(this.Then).Add();
            }
        }
    }
}
