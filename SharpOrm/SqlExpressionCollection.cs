using System.Linq;
using System.Text;

namespace SharpOrm
{
    public class SqlExpressionCollection : SqlExpression
    {
        public SqlExpression[] Expressions { get; }
        private object[] _parms;

        public new object[] Parameters
        {
            get
            {
                if (_parms == null)
                    _parms = Expressions.Select(x => x.Parameters).Aggregate((current, next) => current.Concat(next).ToArray());

                return _parms;
            }
        }

        public SqlExpressionCollection(SqlExpression[] expressions)
        {
            this.Expressions = expressions;
        }

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
