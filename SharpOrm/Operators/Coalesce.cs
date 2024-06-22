using SharpOrm.Builder;
using System;
using System.Linq;

namespace SharpOrm.Operators
{
    /// <summary>
    /// Represents a SQL COALESCE function.
    /// </summary>
    public class Coalesce : Column
    {
        private readonly Column[] columns;

        /// <summary>
        /// Initializes a new instance of the <see cref="Coalesce"/> class with the specified SQL expressions.
        /// </summary>
        /// <param name="expression">The SQL expressions to be included in the COALESCE function.</param>
        /// <exception cref="ArgumentNullException">Thrown if no expressions are provided.</exception>
        public Coalesce(params SqlExpression[] expression)
        {
            if (expression.Length == 0)
                throw new ArgumentNullException(nameof(expression));

            this.columns = expression.Select(x => new Column(x)).ToArray();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Coalesce"/> class with the specified column names.
        /// </summary>
        /// <param name="columns">The column names to be included in the COALESCE function.</param>
        public Coalesce(params string[] columns) : this(columns.Select(x => new Column(x, "")).ToArray())
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Coalesce"/> class with the specified column names.
        /// </summary>
        /// <param name="columns">The column names to be included in the COALESCE function.</param>
        public Coalesce(params Column[] columns)
        {
            if (columns.Length == 0)
                throw new ArgumentNullException(nameof(expression));

            this.columns = columns;
        }

        /// <summary>
        /// Converts the COALESCE function to a SQL expression.
        /// </summary>
        /// <param name="info">The query information.</param>
        /// <param name="alias">A value indicating whether to include an alias.</param>
        /// <returns>The SQL expression representing the COALESCE function.</returns>
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
