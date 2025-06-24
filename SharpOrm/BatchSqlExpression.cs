using System.Linq;
using System.Text;

namespace SharpOrm
{
    /// <summary>
    /// Represents a batch of SQL expressions.
    /// </summary>
    public class BatchSqlExpression : SqlExpression
    {
        /// <summary>
        /// Gets the SQL expressions in the batch.
        /// </summary>
        public SqlExpression[] Expressions { get; }
        private object[] _parms;

        public override bool IsEmpty => Expressions.Length == 0;

        /// <summary>
        /// Gets the parameters used in the SQL expressions.
        /// </summary>
        public new object[] Parameters
        {
            get
            {
                if (_parms == null)
                    _parms = Expressions.Select(x => x.Parameters).Aggregate((current, next) => current.Concat(next).ToArray());

                return _parms;
            }
        }

        /// <summary>
        /// Initializes a new instance of the BatchSqlExpression class with the provided SQL expressions.
        /// </summary>
        /// <param name="expressions">The SQL expressions in the batch.</param>
        public BatchSqlExpression(params SqlExpression[] expressions)
        {
            this.Expressions = expressions;
        }

        /// <summary>
        /// Returns the SQL expressions as a string.
        /// </summary>
        /// <returns>The SQL expressions as a string.</returns>
        public override string ToString()
        {
            if (Expressions.Length == 0)
                return string.Empty;

            StringBuilder builder = new StringBuilder();

            builder.AppendLine(Expressions[0].ToString());

            for (int i = 1; i < Expressions.Length; i++)
                builder.AppendLine("\\").AppendLine(Expressions[i].ToString());

            return builder.ToString();
        }
    }
}
