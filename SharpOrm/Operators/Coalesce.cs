using SharpOrm.Builder;
using System;

namespace SharpOrm.Operators
{
    public class Coalesce : Column
    {
        private readonly SqlExpression[] values;

        public Coalesce(params SqlExpression[] expression)
        {
            if (expression.Length == 0)
                throw new ArgumentNullException(nameof(expression));

            this.values = expression;
        }

        public override SqlExpression ToExpression(IReadonlyQueryInfo info, bool alias)
        {
            QueryConstructor constructor = new QueryConstructor(info);
            constructor.Add("COALESCE(").Add(this.values[0]);

            for (int i = 1; i < this.values.Length; i++)
                constructor.Add(',').Add(this.values[0]);

            constructor.Add(")");

            if (!string.IsNullOrEmpty(this.Alias))
                constructor.Add(" ").Add(this.Alias);

            return constructor.ToExpression(info);
        }
    }
}
