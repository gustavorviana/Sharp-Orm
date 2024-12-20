using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm.Builder.Expressions
{
    internal class ExpressionColumn : Column
    {
        public ExpressionColumn(SqlExpression expression) : base(expression)
        {
        }

        public override SqlExpression ToExpression(IReadonlyQueryInfo info, bool alias)
        {
            QueryBuilder builder = new QueryBuilder(info);

            if (info is QueryInfo qi && qi.Joins.Count > 0)
                builder.Add(info.Config.ApplyNomenclature(info.TableName.TryGetAlias(info.Config))).Add('.');

            builder.Add(this.expression);

            if (!alias || this.expression.ToString() == this.Alias) return builder.ToExpression();

            if (alias && !string.IsNullOrEmpty(this.Alias))
                builder.Add(" AS ").Add(info.Config.ApplyNomenclature(this.Alias));

            return builder.ToExpression();
        }
    }
}
