using System.Text;

namespace SharpOrm.Builder
{
    public class SqlExpression
    {
        protected string value;

        public SqlExpression(string value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return this.value;
        }

        public static explicit operator SqlExpression(StringBuilder builder)
        {
            return new SqlExpression(builder.ToString());
        }

        public static explicit operator SqlExpression(string expression)
        {
            return new SqlExpression(expression);
        }

        public static explicit operator string(SqlExpression expression)
        {
            return expression.ToString();
        }
    }
}
