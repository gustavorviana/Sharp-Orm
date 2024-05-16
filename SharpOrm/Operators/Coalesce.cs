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
            QueryConstructor constructor = new QueryConstructor(info);
            constructor.Add("COALESCE(").AddExpression(this.columns[0], false);

            for (int i = 1; i < this.columns.Length; i++)
                constructor.Add(',').AddExpression(this.columns[i], false);

            constructor.Add(")");

            if (!string.IsNullOrEmpty(this.Alias))
                constructor.Add(" ").Add(this.Alias);

            return constructor.ToExpression(info);
        }
    }
}
