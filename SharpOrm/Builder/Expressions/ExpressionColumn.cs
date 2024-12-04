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
            if (!alias || this.expression.ToString() == this.Alias) return this.expression;

            QueryBuilder builder = new QueryBuilder(info);
            builder.Add(this.expression);

            if (alias && !string.IsNullOrEmpty(this.Alias))
                builder.Add(" AS ").Add(info.Config.ApplyNomenclature(this.Alias));

            return builder.ToExpression();
        }
    }
}
