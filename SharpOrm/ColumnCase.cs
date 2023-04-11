using SharpOrm.Builder;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SharpOrm
{
    public class ColumnCase : Column
    {
        private readonly List<CaseNode> nodes = new List<CaseNode>();
        private object elseValue;

        #region Constructor
        public ColumnCase()
        {
        }

        public ColumnCase(string columnName) : base(columnName)
        {
        }

        public ColumnCase(string columnName, string alias) : base(columnName, alias)
        {
        }

        public ColumnCase(SqlExpression expression) : base(expression)
        {
        }

        public ColumnCase(SqlExpression expression, string alias) : base(expression)
        {
            this.Alias = alias;
        }
        #endregion

        #region When
        public ColumnCase When(string column, string operation, object value, object then)
        {
            return this.When(new SqlExpression($"{column} {operation} ?", value), then);
        }

        public ColumnCase When(object expected, object then)
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
            this.elseValue = value == null ? DBNull.Value : value;
            return this;
        }

        public override SqlExpression ToExpression(IReadonlyQueryInfo info, bool alias)
        {
            var query = new QueryConstructor();
            this.WriteCase(query, info);

            foreach (var node in this.nodes)
                node.WriteTo(query);

            query.Add(" END");

            if (alias && !string.IsNullOrEmpty(this.Alias))
                query.Add($" AS {info.Config.ApplyNomenclature(this.Alias)}");

            return query.ToExpression(info);
        }

        private void WriteCase(QueryConstructor query, IReadonlyQueryInfo info)
        {
            query.Add("CASE");

            if (this.expression != null) query.Add(" ").Add(this.expression);
            else if (!string.IsNullOrEmpty(this.Name)) query.Add($" {info.Config.ApplyNomenclature(this.Name)}");
        }

        private class CaseNode
        {
            public SqlExpression Expression;

            public object Then;

            public void WriteTo(QueryConstructor query)
            {
                query.Add($"WHEN {this.Expression} THEN ");

                if (this.Expression != null)
                    query.AddParams(this.Expression.Parameters);

                if (this.Then is SqlExpression exp) query.Add(exp);
                else query.Add("?").AddParams(this.Then);

                query.Line();
            }
        }
    }
}
