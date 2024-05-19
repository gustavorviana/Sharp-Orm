using SharpOrm.Builder;
using System;
using System.Linq;

namespace SharpOrm.Operators
{
    public class Coalesce : Column
    {
        private readonly Column[] columns;

        public Coalesce(params SqlExpression[] expression)
        {
            if (expression.Length == 0)
                throw new ArgumentNullException(nameof(expression));

            this.columns = expression.Select(x => new Column(x)).ToArray();
        }

        public Coalesce(params string[] columns) : this(columns.Select(x => new Column(x, "")).ToArray())
        {

        }

        public Coalesce(params Column[] columns)
        {
            if (columns.Length == 0)
                throw new ArgumentNullException(nameof(expression));

            this.columns = columns;
        }

        public override SqlExpression ToExpression(IReadonlyQueryInfo info, bool alias)
        {
            QueryBuilder builder = new QueryBuilder(info);
            builder.Add("COALESCE(").AddExpression(this.columns[0], false);

            for (int i = 1; i < this.columns.Length; i++)
                builder.Add(',').AddExpression(this.columns[i], false);

            builder.Add(")");

            if (!string.IsNullOrEmpty(this.Alias))
                builder.Add(" ").Add(this.Alias);

            return builder.ToExpression(info);
        }
    }
}
